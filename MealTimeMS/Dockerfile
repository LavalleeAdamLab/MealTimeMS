FROM mono:latest
RUN mkdir /opt/app
EXPOSE 13000/tcp
COPY bin/x64/Release /opt/app
RUN  apt-get -y install librdkafka
ENTRYPOINT ["mono","/opt/app/MealTimeMS.exe"]
CMD ["-c --kafka_ip localhost --kafka_port 9092 --schema_ip localhost --schema_port 8083 --exclusionMS_ip 192.168.0.29 --exclusionMS_port 8000"]