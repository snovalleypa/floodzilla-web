# FzCommon

This is all of the code that is common between various Floodzilla projects.

## TODO

* Fix all nullable warnings
* Remove all usage of Entity Framework
* Make all data entities exist within a specific Region
* Separate out database save/load code so that it would be more straightforward to use a different database
* Consider replacing Newtonsoft.JSON with builtin .NET JSON functionality
* Don't create SQL connections within methods -- always pass in the SQL connection
