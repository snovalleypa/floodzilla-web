This project was bootstrapped with [Create React App](https://github.com/facebook/create-react-app).

do stuff:
- npm install
- npm run build
- npm run wstart (to run locally on windows)
- copybuild (copies the build output into the FloodzillaWeb project for deployment)

TODO:
* Fix how global JS files are included (see public/index.html)
* Re-add Facebook login; among other things, need to fix how app ID is managed
* Fix global configuration (see src/constants/index.js)
* Remove all site-specific info (see src/lib/usgsInfo.js)
* Rewrite chart building (consolidate all the chart options builders)
