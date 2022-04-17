del /q ..\..\Websites\floodzillaweb\wwwroot\precache-manifest.*
rd /s /q ..\..\Websites\floodzillaweb\wwwroot\img
rd /s /q ..\..\Websites\floodzillaweb\wwwroot\static

copy /y build\* ..\..\Websites\floodzillaweb\wwwroot
xcopy /EI build\img ..\..\Websites\floodzillaweb\wwwroot\img
xcopy /EI build\static ..\..\Websites\floodzillaweb\wwwroot\static

