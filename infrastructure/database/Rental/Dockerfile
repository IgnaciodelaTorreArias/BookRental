FROM postgres
COPY Create.sql .
ENTRYPOINT [ "/bin/sh" ]
CMD ["-c", "while ! PGPASSWORD=$PGPASSWORD psql -h RentalDB -p 5432 -U postgres -f Create.sql; do sleep 10; done;"]