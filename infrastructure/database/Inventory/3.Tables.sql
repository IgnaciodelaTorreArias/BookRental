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