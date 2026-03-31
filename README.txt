RaktarNet Web - böngészőből elérhető raktárkezelő rendszer

Ez a csomag egy ASP.NET Core MVC webalkalmazást tartalmaz.
A rendszer böngészőből érhető el, például:
http://localhost:5000

Kezdő belépés:
Felhasználónév: admin
Jelszó: admin123

Fő funkciók:
- bejelentkezés
- termékek kezelése
- bevételezés / kiadás
- mozgásnapló
- dolgozók kezelése
- áttekintés

INDÍTÁS VISUAL STUDIO-BÓL:
1. Nyisd meg a RaktarNet.Web.csproj fájlt vagy a projektet Visual Studio-ban
2. Indítsd el
3. Böngészőben nyílik meg

INDÍTÁS PARANCSSORBÓL:
1. Nyiss parancssort a projekt mappájában
2. Írd be:
   dotnet restore
   dotnet run

PUBLISH önálló futtatáshoz:
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true

Ezután a publish mappában lesz a futtatható fájl.
