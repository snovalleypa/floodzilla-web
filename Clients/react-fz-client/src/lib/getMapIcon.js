export default function getMapIcon(isOffline, waterStatus, floodLevel) {
  if (!waterStatus) {
    waterStatus = "Offline";
  }

  var icon = getStatusSvg(isOffline, waterStatus);
  if (!isOffline) {
    switch (floodLevel) {
      case 0:
        icon = icon.replace(".cls-2{}", ".cls-2{fill:#9fd140}");
        break;
      case 1:
        icon = icon.replace(".cls-2{}", ".cls-2{fill:#9fd140}");
        break;
      case 2:
        icon = icon.replace(".cls-2{}", ".cls-2{fill:#ffa700}");
        break;
      case 3:
        icon = icon.replace(".cls-2{}", ".cls-2{fill:#ea4b4b}");
        break;
      default:
        console.error("Unknown flood level", floodLevel);
    }
  }

  return icon;
}

const flatIcon = [
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 72 78.14">' +
    "<defs>" +
    "<style>" +
    " .cls-1{fill:rgba(255,255,255,.92)}.cls-2{}.cls-3{filter:url(#Path_255805)}" +
    // styling icon inside svg. e.g cresting, falling etc
    ".cls-4{fill:#fff}" +
    "</style>" +
    '<filter id="Path_255805" width="72" height="78.14" x="0" y="0" filterUnits="userSpaceOnUse">' +
    '<feOffset dy="3"/>' +
    '<feGaussianBlur result="blur" stdDeviation="3"/>' +
    '<feFlood flood-opacity=".161"/>' +
    '<feComposite in2="blur" operator="in"/>' +
    '<feComposite in="SourceGraphic"/>' +
    "</filter>" +
    "</defs>" +
    '<g id="Pin" transform="translate(9 6)">' +
    '<g class="cls-3" transform="translate(-9 -6)">' +
    '<path id="Path_255805-2" d="M27 3a24 24 0 0 0-4.72 47.54c1.73.32 4.79 4.57 4.79 4.57s3.29-4.28 4.59-4.56A24 24 0 0 0 27 3m0-3a26.955 26.955 0 0 1 20.83 44.15 27.334 27.334 0 0 1-6.88 5.93 27.613 27.613 0 0 1-8.32 3.34 26.077 26.077 0 0 0-3.18 3.52l-2.46 3.2-2.36-3.27c0-.01-.58-.8-1.34-1.68a9.923 9.923 0 0 0-1.78-1.73 26.554 26.554 0 0 1-15.25-9.11A27.015 27.015 0 0 1 27 0z" class="cls-1" data-name="Path 255805" transform="translate(9 6)"/>' +
    "</g>" +
    '<path id="Path_255804" d="M27 3a23.995 23.995 0 0 1 4.66 47.54c-1.3.28-4.59 4.56-4.59 4.56s-3.06-4.24-4.79-4.57A24 24 0 0 1 27 3z" class="cls-2" data-name="Path 255804"/>' +
    '<path id="Path_255773" d="M27.867 12.631l-3.72-3.72A.668.668 0 0 0 23 9.377v2.387H4.333a1.333 1.333 0 1 0 0 2.667H23v2.387a.66.66 0 0 0 1.133.467l3.72-3.72a.656.656 0 0 0 .013-.933z" class="cls-4" data-name="Path 255773" transform="translate(13 15)"/>' +
    "</g>" +
    "</svg>",
].join("\n");

const risingIcon = [
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 72 78.14">' +
    "<defs>" +
    "<style>" +
    " .cls-1{fill:rgba(255,255,255,.92)}.cls-2{}.cls-3{filter:url(#Path_255805)}" +
    // styling icon inside svg. e.g cresting, falling etc
    ".cls-4{fill:#ffffff}" +
    "</style>" +
    '<filter id="Path_255805" width="72" height="78.14" x="0" y="0" filterUnits="userSpaceOnUse">' +
    '<feOffset dy="3"/>' +
    '<feGaussianBlur result="blur" stdDeviation="3"/>' +
    '<feFlood flood-opacity=".161"/>' +
    '<feComposite in2="blur" operator="in"/>' +
    '<feComposite in="SourceGraphic"/>' +
    "</filter>" +
    "</defs>" +
    '<g id="Pin" transform="translate(9 6)">' +
    '<g class="cls-3" transform="translate(-9 -6)">' +
    '<path id="Path_255805-2" d="M27 3a24 24 0 0 0-4.72 47.54c1.73.32 4.79 4.57 4.79 4.57s3.29-4.28 4.59-4.56A24 24 0 0 0 27 3m0-3a26.955 26.955 0 0 1 20.83 44.15 27.334 27.334 0 0 1-6.88 5.93 27.613 27.613 0 0 1-8.32 3.34 26.077 26.077 0 0 0-3.18 3.52l-2.46 3.2-2.36-3.27c0-.01-.58-.8-1.34-1.68a9.923 9.923 0 0 0-1.78-1.73 26.554 26.554 0 0 1-15.25-9.11A27.015 27.015 0 0 1 27 0z" class="cls-1" data-name="Path 255805" transform="translate(9 6)"/>' +
    "</g>" +
    '<path id="Path_255804" d="M27 3a23.995 23.995 0 0 1 4.66 47.54c-1.3.28-4.59 4.56-4.59 4.56s-3.06-4.24-4.79-4.57A24 24 0 0 1 27 3z" class="cls-2" data-name="Path 255804"/>' +
    '<path class="cls-4" id="Path_255771" d="M21.633 7.141l1.917 1.932-6.5 6.549-4.38-4.415a1.318 1.318 0 0 0-1.877 0l-7.986 8.065a1.344 1.344 0 0 0 0 1.892 1.318 1.318 0 0 0 1.877 0l7.043-7.113 4.38 4.415a1.318 1.318 0 0 0 1.877 0l7.442-7.488 1.917 1.932a.664.664 0 0 0 1.132-.47V6.671A.643.643 0 0 0 27.823 6h-5.711a.672.672 0 0 0-.479 1.141z" data-name="Path 255771" transform="translate(10.827 13.951)"/>' +
    "</g>" +
    "</svg>",
].join("\n");

const fallingIcon = [
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 72 78.14">' +
    "<defs>" +
    "<style>" +
    " .cls-1{fill:rgba(255,255,255,.92)}.cls-2{}.cls-3{filter:url(#Path_255805)}" +
    // styling icon inside svg. e.g cresting, falling etc
    ".cls-4{fill:#ffffff}" +
    "</style>" +
    '<filter id="Path_255805" width="72" height="78.14" x="0" y="0" filterUnits="userSpaceOnUse">' +
    '<feOffset dy="3"/>' +
    '<feGaussianBlur result="blur" stdDeviation="3"/>' +
    '<feFlood flood-opacity=".161"/>' +
    '<feComposite in2="blur" operator="in"/>' +
    '<feComposite in="SourceGraphic"/>' +
    "</filter>" +
    "</defs>" +
    '<g id="Pin" transform="translate(9 6)">' +
    '<g class="cls-3" transform="translate(-9 -6)">' +
    '<path id="Path_255805-2" d="M27 3a24 24 0 0 0-4.72 47.54c1.73.32 4.79 4.57 4.79 4.57s3.29-4.28 4.59-4.56A24 24 0 0 0 27 3m0-3a26.955 26.955 0 0 1 20.83 44.15 27.334 27.334 0 0 1-6.88 5.93 27.613 27.613 0 0 1-8.32 3.34 26.077 26.077 0 0 0-3.18 3.52l-2.46 3.2-2.36-3.27c0-.01-.58-.8-1.34-1.68a9.923 9.923 0 0 0-1.78-1.73 26.554 26.554 0 0 1-15.25-9.11A27.015 27.015 0 0 1 27 0z" class="cls-1" data-name="Path 255805" transform="translate(9 6)"/>' +
    "</g>" +
    '<path id="Path_255804" d="M27 3a23.995 23.995 0 0 1 4.66 47.54c-1.3.28-4.59 4.56-4.59 4.56s-3.06-4.24-4.79-4.57A24 24 0 0 1 27 3z" class="cls-2" data-name="Path 255804"/>' +
    '<path id="Path_255769" d="M21.661 20.731l1.92-1.92-6.507-6.511-4.387 4.387a1.328 1.328 0 0 1-1.88 0l-8-8.013A1.329 1.329 0 0 1 4.688 6.8l7.053 7.067 4.387-4.387a1.328 1.328 0 0 1 1.88 0l7.453 7.44 1.92-1.92a.666.666 0 0 1 1.133.467v5.72a.66.66 0 0 1-.667.667h-5.72a.655.655 0 0 1-.467-1.12z" class="cls-4" data-name="Path 255769" transform="translate(10.827 13.951)"/>' +
    "</g>" +
    "</svg>",
].join("\n");

const offlineIcon = [
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 72 78.14">' +
    "<defs>" +
    "<style>" +
    ".cls-1{fill:rgba(255,255,255,.92)}.cls-2{fill:#b1b1b1}.cls-3{filter:url(#Path_255805)}" +
    // styling icon inside svg. e.g cresting, falling etc
    ".cls-4{fill:#fff}" +
    "</style>" +
    '<filter id="Path_255805" width="72" height="78.14" x="0" y="0" filterUnits="userSpaceOnUse">' +
    '<feOffset dy="3"/>' +
    '<feGaussianBlur result="blur" stdDeviation="3"/>' +
    '<feFlood flood-opacity=".161"/>' +
    '<feComposite in2="blur" operator="in"/>' +
    '<feComposite in="SourceGraphic"/>' +
    "</filter>" +
    "</defs>" +
    '<g id="Pin" transform="translate(9 6)">' +
    '<g class="cls-3" transform="translate(-9 -6)">' +
    '<path id="Path_255805-2" d="M27 3a24 24 0 0 0-4.72 47.54c1.73.32 4.79 4.57 4.79 4.57s3.29-4.28 4.59-4.56A24 24 0 0 0 27 3m0-3a26.955 26.955 0 0 1 20.83 44.15 27.334 27.334 0 0 1-6.88 5.93 27.613 27.613 0 0 1-8.32 3.34 26.077 26.077 0 0 0-3.18 3.52l-2.46 3.2-2.36-3.27c0-.01-.58-.8-1.34-1.68a9.923 9.923 0 0 0-1.78-1.73 26.554 26.554 0 0 1-15.25-9.11A27.015 27.015 0 0 1 27 0z" class="cls-1" data-name="Path 255805" transform="translate(9 6)"/>' +
    "</g>" +
    '<path id="Path_255804" d="M27 3a23.995 23.995 0 0 1 4.66 47.54c-1.3.28-4.59 4.56-4.59 4.56s-3.06-4.24-4.79-4.57A24 24 0 0 1 27 3z" class="cls-2" data-name="Path 255804"/>' +
    '<path id="Path_255797" d="M17.56 14.24a5.987 5.987 0 0 0-7.8-7.8l1.62 1.62A4.2 4.2 0 0 1 12 8a4 4 0 0 1 4 4 4.457 4.457 0 0 1-.05.63zM12 4a8 8 0 0 1 8 8 7.9 7.9 0 0 1-.95 3.74l1.47 1.47A9.861 9.861 0 0 0 22 12 9.993 9.993 0 0 0 6.79 3.47l1.46 1.46A8.039 8.039 0 0 1 12 4zM3.27 2.5L2 3.77l2.1 2.1a9.994 9.994 0 0 0 2.89 14.78l1-1.73A8.005 8.005 0 0 1 4 12a7.9 7.9 0 0 1 1.53-4.69l1.43 1.44A5.987 5.987 0 0 0 9 17.19l1-1.74a3.942 3.942 0 0 1-1.56-5.24l1.58 1.58L10 12a2.006 2.006 0 0 0 2 2l.21-.02.01.01 7.51 7.51L21 20.23 4.27 3.5z" class="cls-4" data-name="Path 255797" transform="translate(15 15)"/>' +
    "</g>" +
    "</svg>",
].join("\n");

function getStatusSvg(isOffline, waterStatus) {
  if (!isOffline) {
    switch (waterStatus) {
      case "Cresting":
      case "Steady":
        return flatIcon;
      case "Falling":
        return fallingIcon;
      case "Rising":
        return risingIcon;
      case "Offline":
        return offlineIcon;
      case "Status not found.":
        return flatIcon;
      default:
        console.error("Unknown water status", waterStatus);
        return offlineIcon;
    }
  } else {
    return offlineIcon;
  }
}
