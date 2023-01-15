FROM mono:latest
RUN mkdir /opt/app
COPY . /opt/app
WORKDIR /opt/app
RUN msbuild MealTimeMS.sln /property:Configuration=Release /property:Platform=x64
RUN export MONO_PATH=MealTimeMS/bin/x64/Release/librdkafka/x64/
ENTRYPOINT ["mono","MealTimeMS/bin/x64/Release/MealTimeMS.exe"]
