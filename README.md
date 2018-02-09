# Lykke.Service.Stellar.Api

# Tests
End-to-end tests are available as postman collection. Can be run directly in [postman](https://www.getpostman.com/) or on the command line.
* Install newman:
```sh
npm install -g newman
```
* Start Api and Sign services
* Set URLs in the pre-request script of INIT to point to the Api and Sign service:
```javascript
// set global variables
pm.globals.set("URL", "http://localhost:5000");
pm.globals.set("URL_SIGN", "http://localhost:5001");
```
* Start tests:
```sh
newman run LykkeStellarApiTests.postman_collection.json
```
