FROM postgres
COPY Create.sql .
ENTRYPOINT [ "/bin/sh" ]
CMD ["-c", "while ! PGPASSWORD=$PGPASSWORD psql -h InventoryDB -p 5432 -U postgres -f Create.sql; do sleep 10; done;"]