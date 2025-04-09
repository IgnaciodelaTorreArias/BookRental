CREATE DATABASE "Inventory"
    WITH
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    LOCALE_PROVIDER = 'libc'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1
    IS_TEMPLATE = False;

\c "Inventory";

CREATE SCHEMA IF NOT EXISTS inventory
    AUTHORIZATION postgres;

CREATE ROLE "API" WITH
  LOGIN
  NOSUPERUSER
  INHERIT
  NOCREATEDB
  NOCREATEROLE
  NOREPLICATION
  NOBYPASSRLS
  ENCRYPTED PASSWORD 'SCRAM-SHA-256$4096:cwi3hnrrgdYGtr56Uy7xiQ==$ptapnVqME/ARvY9F8+c3NxQl+qniIgs1T+OOLHZXB+k=:or/8+tzF/WuDFb3MM8rS0z5Uhe4kLllMaC+zpenc3VM=';

CREATE ROLE analysis_inventory WITH
  LOGIN
  NOSUPERUSER
  INHERIT
  NOCREATEDB
  NOCREATEROLE
  NOREPLICATION
  NOBYPASSRLS
  ENCRYPTED PASSWORD 'SCRAM-SHA-256$4096:VbW0acpJJfvJTU6XyB6Ndw==$J7oJSjpUkDxocjunymLAY5NDWXpHYCw4jJtOObVi1uo=:ALMu8MlejRBdJ5Gsm3OUvlgnEvgL+aA6HcbaJ133Rqc=';

GRANT CONNECT ON DATABASE "Inventory" TO "API";
GRANT CONNECT ON DATABASE "Inventory" TO analysis_inventory;
GRANT ALL ON DATABASE "Inventory" TO postgres;

GRANT USAGE ON SCHEMA inventory TO "API";
GRANT USAGE ON SCHEMA inventory TO analysis_inventory;
GRANT ALL ON SCHEMA inventory TO postgres;

CREATE TABLE inventory.books
(
    book_id integer NOT NULL GENERATED ALWAYS AS IDENTITY,
    book_name character varying(255) NOT NULL,
    iso_language_code character(3) NOT NULL,
    author_name character varying(255) NOT NULL DEFAULT '',
    published_date date,
    description text NOT NULL DEFAULT '',
    rental_fee integer NOT NULL DEFAULT 100,
    visible boolean NOT NULL DEFAULT true,
    PRIMARY KEY (book_id)
);

ALTER TABLE IF EXISTS inventory.books
    OWNER to postgres;

GRANT SELECT, INSERT, UPDATE ON TABLE inventory.books TO "API";
GRANT SELECT ON TABLE inventory.books TO analysis_inventory;
GRANT ALL ON TABLE inventory.books TO postgres;

--------------------

CREATE TYPE inventory.acquisition_status AS ENUM
    ('unconfirmed', 'confirmed', 'error');

ALTER TYPE inventory.acquisition_status
    OWNER TO postgres;

CREATE TABLE inventory.acquisitions
(
    acquisition_id integer NOT NULL GENERATED ALWAYS AS IDENTITY,
    book_id integer NOT NULL,
    acquisition_price integer NOT NULL,
    quantity integer NOT NULL,
    acquisition_date date NOT NULL DEFAULT CURRENT_DATE,
    acquisition_status inventory.acquisition_status NOT NULL DEFAULT 'unconfirmed',
    copy_id_start integer NOT NULL,
    PRIMARY KEY (acquisition_id),
    FOREIGN KEY (book_id)
        REFERENCES inventory.books (book_id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
        NOT VALID
);

ALTER TABLE IF EXISTS inventory.acquisitions
    OWNER to postgres;

GRANT SELECT, INSERT, UPDATE ON TABLE inventory.acquisitions TO "API";
GRANT SELECT ON TABLE inventory.acquisitions TO analysis_inventory;
GRANT ALL ON TABLE inventory.acquisitions TO postgres;

--------------------

CREATE TYPE inventory.copy_status AS ENUM
    ('available', 'unavailable', 'lost', 'retired');

ALTER TYPE inventory.copy_status
    OWNER TO postgres;

CREATE TABLE inventory.stock
(
    copy_id integer NOT NULL GENERATED ALWAYS AS IDENTITY,
    book_id integer NOT NULL,
    acquisition_id integer NOT NULL,
    copy_status inventory.copy_status NOT NULL DEFAULT 'unavailable',
    PRIMARY KEY (copy_id)
);

ALTER TABLE IF EXISTS inventory.stock
    OWNER to postgres;

GRANT SELECT, UPDATE ON TABLE inventory.stock TO "API";
GRANT SELECT ON TABLE inventory.stock TO analysis_inventory;
GRANT ALL ON TABLE inventory.stock TO postgres;

CREATE OR REPLACE FUNCTION inventory.acquisitions_before_insert()
    RETURNS trigger
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE NOT LEAKPROOF SECURITY DEFINER
AS $BODY$
BEGIN
	WITH stock AS (
		INSERT INTO inventory.stock (book_id, acquisition_id)
		SELECT book_id, acquisition_id
		FROM (SELECT NEW.book_id, NEW.acquisition_id)
		CROSS JOIN generate_series(1, NEW.quantity)
		RETURNING copy_id
	)
	SELECT * FROM stock LIMIT 1 INTO NEW.copy_id_start;
	RETURN NEW;
END;
$BODY$;

ALTER FUNCTION inventory.acquisitions_before_insert()
    OWNER TO postgres;

CREATE OR REPLACE TRIGGER stock_creation
    BEFORE INSERT
    ON inventory.acquisitions
    FOR EACH ROW
    EXECUTE FUNCTION inventory.acquisitions_before_insert();

--------------------

CREATE FUNCTION inventory.acquisitions_before_status_update()
    RETURNS trigger
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE NOT LEAKPROOF SECURITY DEFINER
AS $BODY$
BEGIN
    IF OLD.acquisition_status <> 'unconfirmed' THEN
		RETURN OLD;
	END IF;
	IF NEW.acquisition_status = 'confirmed' THEN
		UPDATE inventory.stock
		SET copy_status = 'available'
		WHERE acquisition_id = NEW.acquisition_id;
	END IF;
	IF NEW.acquisition_status = 'error' THEN
		DELETE
		FROM inventory.stock
		WHERE acquisition_id = NEW.acquisition_id;
	END IF;
	RETURN NEW;
END;
$BODY$;

ALTER FUNCTION inventory.acquisitions_before_status_update()
    OWNER TO postgres;

CREATE OR REPLACE TRIGGER stock_update
    BEFORE UPDATE OF acquisition_status
    ON inventory.acquisitions
    FOR EACH ROW
    EXECUTE FUNCTION inventory.acquisitions_before_status_update();

CREATE OR REPLACE FUNCTION inventory.get_book_lease(
	book integer)
    RETURNS integer
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE SECURITY DEFINER PARALLEL UNSAFE 
AS $BODY$
DECLARE
	res int;
BEGIN
	UPDATE inventory.stock
	SET copy_status = 'unavailable'
	WHERE copy_id = (
		SELECT copy_id
		FROM inventory.stock
		WHERE book_id = book AND copy_status = 'available'
		FOR UPDATE SKIP LOCKED
		LIMIT 1
	)
    RETURNING copy_id INTO res;
	RETURN res;
END;
$BODY$;

ALTER FUNCTION inventory.get_book_lease(integer)
    OWNER TO postgres;

GRANT EXECUTE ON FUNCTION inventory.get_book_lease(integer) TO "API";
GRANT EXECUTE ON FUNCTION inventory.get_book_lease(integer) TO postgres;

--------------------

CREATE OR REPLACE FUNCTION inventory.update_acquisition_quantity(
	acquisition integer,
	new_quantity integer)
    RETURNS integer
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
DECLARE
	res int;
BEGIN
	INSERT INTO inventory.acquisitions (book_id, acquisition_price, quantity, acquisition_date)
	SELECT book_id, acquisition_price, new_quantity, acquisition_date
	FROM inventory.acquisitions
	WHERE acquisition_id = acquisition
	RETURNING acquisiton_id INTO res;

	UPDATE inventory.acquisitions
	SET acquisition_status = 'error'
	WHERE acquisition_id = acquisition;

	RETURN res;
END;
$BODY$;

ALTER FUNCTION inventory.update_acquisition_quantity(integer, integer)
    OWNER TO postgres;

GRANT EXECUTE ON FUNCTION inventory.update_acquisition_quantity(integer, integer) TO "API";
GRANT EXECUTE ON FUNCTION inventory.update_acquisition_quantity(integer, integer) TO postgres;

CREATE INDEX IF NOT EXISTS stock_acquisition_id
    ON inventory.stock USING hash
    (acquisition_id)
    TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS stock_book_id_copy_status_idx
    ON inventory.stock USING btree
    (book_id ASC NULLS LAST, copy_status ASC NULLS LAST)
    WITH (deduplicate_items=True)
    TABLESPACE pg_default;
