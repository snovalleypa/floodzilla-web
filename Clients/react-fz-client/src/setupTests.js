const noop = () => {};
Object.defineProperty(window, "scrollTo", { value: noop, writable: true });
global.fetch = require("jest-fetch-mock");
