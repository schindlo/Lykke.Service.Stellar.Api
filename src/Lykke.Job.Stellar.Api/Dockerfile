FROM microsoft/dotnet:2.1-aspnetcore-runtime
WORKDIR /app
COPY . .
EXPOSE 5000/tcp
ENTRYPOINT ["dotnet", "Lykke.Job.Stellar.Api.dll"]
