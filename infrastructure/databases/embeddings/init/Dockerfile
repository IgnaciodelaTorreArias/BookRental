FROM curlimages/curl:latest
WORKDIR /app
ADD init.sh .
ADD BookRentalCA.crt .
ADD embeddings.crt .
ADD embeddings.key .
ENTRYPOINT [ "/bin/sh", "init.sh" ]