# oagunth

This is a toy project, a lot (and I do mean a lot) of things are missing. Also the code is not of high quality, there was no aim other than tinkering with some tech and illustrate some points for a talk I've given.

To run this locally, with the iOS client, you need to create an SSL certificate, with a local authority.

I used [mkcert](https://github.com/FiloSottile/mkcert) myself.
After installing it, run `mkcert -install` then `mkcert -pkcs12 -p12-file <some file>.pfx localhost`
Then extracted the root certificate of _mkcert_ (cer format) from the Keychain.


Then :
- Create a profile for iOS using *Apple Configurator (2)* set only the root certificate
- Add the profile to iOS simulator
- Trust the profile in iOS simulator (do not forget to go trust the root certificate that comes with it)
- Then configure Kestrel to use this certificate

More details here : https://gist.github.com/ckpearson/f96074697c583de0d2599c6009191824
With mkcert you can ignore steps from 1 to 7 included.

## Server

This project uses, .NET Core, F# (with [Saturn Framework](https://saturnframework.org/) and MongoDB.

In the __server__ folder, run :
- `dotnet tool restore`
- `dotnet paket install`
- set the connection string to your MongoDb instance, using [secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-3.1&tabs=windows). The command is `dotnet user-secrets set "mongoDbUrl" "<your connection string>"`
- set the certificate you created earlier, either change the variable "Kestrel:Certificates:Default:Path", in the *appsettings.Development.json* file of the  _oagunth-api_ folder to the location of your certificate file created earlier (*.pfx) or save the file in the current _oagunth-api_ folder
- then use dotnet run to run the oagunth-api project, the api runs on the port 8080

## Client

Make sure you have installed the profile and trusted the root certificate in the iOS simulator as required above.

Then open the XCode project located somehwere in the __client__ folder.
Run it.

** It's using SwiftUI so only iOS 13 and above are supported **



