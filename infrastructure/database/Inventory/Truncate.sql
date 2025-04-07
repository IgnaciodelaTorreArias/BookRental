DROP TABLE inventory.stock;
DROP TABLE inventory.acquisitions;
DROP TABLE inventory.books;

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

--------------------

CREATE OR REPLACE TRIGGER stock_creation
    BEFORE INSERT
    ON inventory.acquisitions
    FOR EACH ROW
    EXECUTE FUNCTION inventory.acquisitions_before_insert();

--------------------

CREATE OR REPLACE TRIGGER stock_update
    BEFORE UPDATE OF acquisition_status
    ON inventory.acquisitions
    FOR EACH ROW
    EXECUTE FUNCTION inventory.acquisitions_before_status_update();

--------------------

CREATE INDEX IF NOT EXISTS stock_acquisition_id
    ON inventory.stock USING hash
    (acquisition_id)
    TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS stock_book_id_copy_status_idx
    ON inventory.stock USING btree
    (book_id ASC NULLS LAST, copy_status ASC NULLS LAST)
    WITH (deduplicate_items=True)
    TABLESPACE pg_default;