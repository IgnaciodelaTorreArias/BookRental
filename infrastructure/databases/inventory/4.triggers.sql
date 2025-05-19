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