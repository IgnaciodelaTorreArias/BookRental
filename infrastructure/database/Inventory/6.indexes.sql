CREATE INDEX IF NOT EXISTS stock_acquisition_id
    ON inventory.stock USING hash
    (acquisition_id)
    TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS stock_book_id_copy_status_idx
    ON inventory.stock USING btree
    (book_id ASC NULLS LAST, copy_status ASC NULLS LAST)
    WITH (deduplicate_items=True)
    TABLESPACE pg_default;
