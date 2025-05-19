CREATE DATABASE "Rental"
    WITH
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    LOCALE_PROVIDER = 'libc'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1
    IS_TEMPLATE = False;

\c "Rental";

CREATE SCHEMA IF NOT EXISTS rental
    AUTHORIZATION postgres;
CREATE ROLE "API" WITH
  LOGIN
  NOSUPERUSER
  INHERIT
  NOCREATEDB
  NOCREATEROLE
  NOREPLICATION
  NOBYPASSRLS
  ENCRYPTED PASSWORD 'SCRAM-SHA-256$4096:IviSnJ0ANg6uLtnMUh05jQ==$Erp4pkM8L1hUSN7igBgxSNbqlbFlHH6ER9kiaCNl1B0=:j87jDvf9ENrlWUJVpHYbLtd3NIkDDAOt8KTVTwS2V/I=';

CREATE ROLE analysis_rental WITH
  LOGIN
  NOSUPERUSER
  INHERIT
  NOCREATEDB
  NOCREATEROLE
  NOREPLICATION
  NOBYPASSRLS
  ENCRYPTED PASSWORD 'SCRAM-SHA-256$4096:oMdDSTiUjlPeYD72+Q3HhA==$C6aZnhoju2gUX89d2nRQ5IGf9Tyt14S85jXPjLNyYVM=:OLRbf/2oS2wgTsTY0OgolhuWfUjmVWrjKZ9NirvsgkI=';

GRANT CONNECT ON DATABASE "Rental" TO "API";
GRANT CONNECT ON DATABASE "Rental" TO analysis_rental;
GRANT ALL ON DATABASE "Rental" TO postgres;

GRANT USAGE ON SCHEMA rental TO "API";
GRANT USAGE ON SCHEMA rental TO analysis_rental;
GRANT ALL ON SCHEMA rental TO postgres;

CREATE TABLE rental.books
(
    book_id integer NOT NULL,
    rental_fee integer NOT NULL,
    visible boolean NOT NULL,
    PRIMARY KEY (book_id)
);

ALTER TABLE IF EXISTS rental.books
    OWNER to postgres;

GRANT SELECT, INSERT, UPDATE ON TABLE rental.books TO "API";
GRANT ALL ON TABLE rental.books TO postgres;

-----------------------

CREATE TABLE rental.waiting_list
(
    waiting_id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    book_id integer NOT NULL,
    user_id integer NOT NULL,
    joined_at timestamp without time zone NOT NULL DEFAULT NOW(),
    PRIMARY KEY (waiting_id)
);

ALTER TABLE IF EXISTS rental.waiting_list
    OWNER to postgres;

GRANT SELECT, INSERT, DELETE ON TABLE rental.waiting_list TO "API";
GRANT ALL ON TABLE rental.waiting_list TO postgres;

-----------------------

CREATE TABLE rental.notified
(
    notification_id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    book_id integer NOT NULL,
    copy_id integer NOT NULL,
    user_id integer NOT NULL,
    rental_fee integer NOT NULL,
    notified_at timestamp without time zone NOT NULL DEFAULT NOW(),
    PRIMARY KEY (notification_id)
);

ALTER TABLE IF EXISTS rental.notified
    OWNER to postgres;

GRANT SELECT, INSERT, DELETE ON TABLE rental.notified TO "API";
GRANT ALL ON TABLE rental.notified TO postgres;

-----------------------

CREATE TABLE rental.confirmed_rentals
(
    identifier bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    book_id integer NOT NULL,
    copy_id integer NOT NULL,
    user_id integer NOT NULL,
    rental_fee integer NOT NULL,
    confirmed_at timestamp without time zone NOT NULL DEFAULT now(),
    PRIMARY KEY (identifier)
);

ALTER TABLE IF EXISTS rental.confirmed_rentals
    OWNER to postgres;

GRANT SELECT, UPDATE, DELETE ON TABLE rental.confirmed_rentals TO "API";
GRANT ALL ON TABLE rental.confirmed_rentals TO postgres;

-----------------------

CREATE TABLE rental.wait_time_analysis
(
    identifier bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    book_id integer NOT NULL,
    waited_since timestamp without time zone NOT NULL,
    waited_for interval NOT NULL,
    PRIMARY KEY (identifier)
);

ALTER TABLE IF EXISTS rental.wait_time_analysis
    OWNER to postgres;

GRANT SELECT ON TABLE rental.wait_time_analysis TO analysis_rental;
GRANT ALL ON TABLE rental.wait_time_analysis TO postgres;

-----------------------

CREATE TABLE rental.delivery_time_analysis
(
    identifier bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    rental_id bigint NOT NULL,
    delivery_person integer NOT NULL,
    started_at timestamp without time zone NOT NULL,
    delivery_time interval NOT NULL,
    PRIMARY KEY (identifier)
);

ALTER TABLE IF EXISTS rental.delivery_time_analysis
    OWNER to postgres;

GRANT SELECT ON TABLE rental.delivery_time_analysis TO analysis_rental;
GRANT ALL ON TABLE rental.delivery_time_analysis TO postgres;

-----------------------

CREATE TABLE rental.rentals
(
    rental_id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
    user_id integer NOT NULL,
    book_id integer NOT NULL,
    copy_id integer NOT NULL,
    start_date date NOT NULL,
    return_date date,
    rental_fee integer NOT NULL,
    total integer,
    quarter integer NOT NULL,
    PRIMARY KEY (rental_id, quarter)
);

ALTER TABLE IF EXISTS rental.rentals
    OWNER to postgres;

GRANT INSERT, UPDATE ON TABLE rental.rentals TO "API";
GRANT SELECT ON TABLE rental.rentals TO analysis_rental;
GRANT ALL ON TABLE rental.rentals TO postgres;

COMMENT ON COLUMN rental.rentals.quarter
    IS 'FLOOR(extract(YEAR FROM start_date)*10+(extract(MONTH FROM start_date)-1)/3)';

CREATE OR REPLACE FUNCTION rental.get_user_to_notify(
	book integer,
	book_copy integer)
    RETURNS integer
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE SECURITY DEFINER PARALLEL UNSAFE
AS $BODY$
DECLARE
	selected_book RECORD;
	selected_record RECORD;
BEGIN
	SELECT * INTO selected_book
	FROM rental.books
	WHERE book_id = book AND visible = TRUE;

	IF selected_book IS NULL THEN
		RETURN NULL;
	END IF;

    DELETE FROM rental.waiting_list
    WHERE waiting_id IN (
		SELECT waiting_id
		FROM rental.waiting_list
		WHERE book_id = book
		ORDER BY waiting_id ASC
		LIMIT 1
	)
    RETURNING * INTO selected_record;
	
	IF selected_record IS NULL THEN
        RETURN NULL;
    END IF;
	
	INSERT INTO rental.wait_time_analysis (book_id, waited_since, waited_for)
	VALUES (book, selected_record.joined_at, NOW()-selected_record.joined_at);

	INSERT INTO rental.notified (book_id, copy_id, user_id, rental_fee, notified_at)
	VALUES (book, book_copy, selected_record.user_id, selected_book.rental_fee, NOW() + INTERVAL '30 minutes');

	RETURN selected_record.user_id;
END;
$BODY$;

ALTER FUNCTION rental.get_user_to_notify(integer, integer)
    OWNER TO postgres;

GRANT EXECUTE ON FUNCTION rental.get_user_to_notify(integer, integer) TO "API";
GRANT EXECUTE ON FUNCTION rental.get_user_to_notify(integer, integer) TO postgres;

--------------------

CREATE OR REPLACE FUNCTION rental.reassign_expired_notification(
	)
    RETURNS integer
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE SECURITY DEFINER PARALLEL UNSAFE
AS $BODY$
DECLARE
	selected_record RECORD;
	selected_user int;
BEGIN
	DELETE FROM rental.notified
	WHERE notification_id IN (
		SELECT notification_id
		FROM rental.notified
		WHERE notified_at < NOW() - INTERVAL '24 hours'
		ORDER BY notification_id ASC
		LIMIT 1
	)
	RETURNING * INTO selected_record;

	IF selected_record IS NULL THEN
		RETURN NULL;
	END IF;

	SELECT rental.get_user_to_notify(selected_record.book_id, selected_record.copy_id) INTO selected_user;

	IF selected_user IS NULL THEN
		RETURN -selected_record.copy_id;
	END IF;
	
	RETURN selected_record.copy_id;

END;
$BODY$;

ALTER FUNCTION rental.reassign_expired_notification()
    OWNER TO postgres;

GRANT EXECUTE ON FUNCTION rental.reassign_expired_notification() TO "API";
GRANT EXECUTE ON FUNCTION rental.reassign_expired_notification() TO postgres;

--------------------

CREATE OR REPLACE FUNCTION rental.user_confirmed_rental(
	book_copy integer,
	confirmer_user integer)
    RETURNS boolean
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE SECURITY DEFINER PARALLEL UNSAFE
AS $BODY$
DECLARE
	selected_record RECORD;
BEGIN
    DELETE FROM rental.notified
    WHERE copy_id = book_copy AND user_id = confirmer_user
    RETURNING * INTO selected_record;
	
	IF selected_record IS NULL THEN
		-- Too late, the reservation expired
        RETURN FALSE;
    END IF;
	
	INSERT INTO rental.confirmed_rentals (book_id, copy_id, user_id, rental_fee)
	VALUES (selected_record.book_id, book_copy, confirmer_user, selected_record.rental_fee);
	RETURN TRUE;
END;
$BODY$;

ALTER FUNCTION rental.user_confirmed_rental(integer, integer)
    OWNER TO postgres;

GRANT EXECUTE ON FUNCTION rental.user_confirmed_rental(integer, integer) TO "API";
GRANT EXECUTE ON FUNCTION rental.user_confirmed_rental(integer, integer) TO postgres;

--------------------

CREATE FUNCTION rental.initiate_rental(IN confirmed_rental_id bigint, IN delivery_person integer)
    RETURNS bigint
    LANGUAGE 'plpgsql'
    VOLATILE SECURITY DEFINER PARALLEL UNSAFE
AS $BODY$
DECLARE
	selected_record RECORD;
	rental_identifier bigint;
BEGIN
	DELETE FROM rental.confirmed_rentals
	WHERE identifier = confirmed_rental_id
	RETURNING * INTO selected_record;

	IF selected_record IS NULL THEN
		RETURN NULL;
	END IF;
	
	INSERT INTO rental.rentals (user_id, book_id, copy_id, rental_fee, start_date, quarter)
	VALUES (
		selected_record.user_id,
		selected_record.book_id,
		selected_record.copy_id,
		selected_record.rental_fee,
		CURRENT_DATE,
		FLOOR(extract(YEAR FROM CURRENT_DATE)*10+(extract(MONTH FROM CURRENT_DATE)-1)/3)
	)
	RETURNING rental_id INTO rental_identifier;

	INSERT INTO rental.delivery_time_analysis (rental_id, delivery_person, started_at, delivery_time)
	VALUES (
		rental_identifier,
		delivery_person,
		selected_record.confirmed_at,
		NOW()-selected_record.confirmed_at
	);
	RETURN rental_identifier;
END;
$BODY$;

ALTER FUNCTION rental.initiate_rental(bigint, integer)
    OWNER TO postgres;

GRANT EXECUTE ON FUNCTION rental.initiate_rental(bigint, integer) TO "API";
GRANT EXECUTE ON FUNCTION rental.initiate_rental(bigint, integer) TO postgres;

CREATE INDEX IF NOT EXISTS waiting_list_book_id_idx
    ON rental.waiting_list USING hash
    (book_id)
    TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS waiting_list_user_id_idx
    ON rental.waiting_list USING hash
    (user_id)
    TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS notified_user_id_idx
    ON rental.notified USING hash
    (user_id)
    TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS confirmed_rentals_user_id_idx
    ON rental.confirmed_rentals USING hash
    (user_id)
    TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS rentals_user_id_idx
    ON rental.rentals USING hash
    (user_id)
    TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS notified_copy_id_user_id_idx
    ON rental.notified USING btree
    (copy_id ASC NULLS LAST, user_id ASC NULLS LAST)
    WITH (deduplicate_items=True)
    TABLESPACE pg_default;