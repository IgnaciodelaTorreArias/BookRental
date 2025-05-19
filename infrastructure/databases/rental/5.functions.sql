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
    RETURNS integer
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
        RETURN NULL;
    END IF;
	
	INSERT INTO rental.confirmed_rentals (book_id, copy_id, user_id, rental_fee)
	VALUES (selected_record.book_id, book_copy, confirmer_user, selected_record.rental_fee);
	RETURN selected_record.book_id;
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
GRANT EXECUTE ON FUNCTION rental.initiate_rental(integer, integer) TO postgres;