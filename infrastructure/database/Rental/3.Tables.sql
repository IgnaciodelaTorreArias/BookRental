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