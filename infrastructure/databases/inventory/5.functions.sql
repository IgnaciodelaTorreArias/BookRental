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