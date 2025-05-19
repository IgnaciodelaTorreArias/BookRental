FROM postgres
COPY create.sql .
ENTRYPOINT [ "/bin/sh" ]
CMD ["-c", "while ! PGPASSWORD=$PGPASSWORD psql -h InventoryDB -p 5432 -U postgres -f create.sql; do sleep 10; done;"]