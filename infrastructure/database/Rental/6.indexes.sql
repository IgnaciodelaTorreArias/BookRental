CREATE INDEX IF NOT EXISTS waiting_list_book_id_idx
    ON rental.waiting_list USING hash
    (book_id)
    TABLESPACE pg_default;

CREATE INDEX IF NOT EXISTS notified_copy_id_user_id_idx
    ON rental.notified USING btree
    (copy_id ASC NULLS LAST, user_id ASC NULLS LAST)
    WITH (deduplicate_items=True)
    TABLESPACE pg_default;