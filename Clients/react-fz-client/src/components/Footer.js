import React from "react";
import Constants from "../constants";
import { Link } from "react-router-dom";

export default function Footer() {
  return (
    <footer>
      <div className="row">
        <div
          className="col-lg-9 col-md-8 col-sm-8 col-xs-12"
          id="footer-main-content"
        >
          <p className="text-justify">
            The Floodzilla Gage Network is maintained by{" "}
            <a href="https://svpa.us">
              The Snoqualmie Valley Preservation Alliance
            </a>
            . The Snoqualmie Valley Preservation Alliance is a 501(c)(3)
            nonprofit organization committed to protecting the viability of
            farms, residents, and businesses of the beautiful Snoqualmie River
            Valley. This site is made possible by the countless volunteer hours
            of the local technology team, SVPA donors, and a generous grant from
            King County Flood Control District.
          </p>
        </div>
        <div className="col-lg-3 col-md-4 col-sm-4 col-xs-12">
          <a href="https://svpa.us">
            <img
              src={`${Constants.RESOURCE_BASE_URL}/images/DashboardIcons/SVPA_Logo_FullColor_300px.png`}
              alt="SVPA Logo"
            />
          </a>
        </div>
      </div>
      <hr />
      <div className="row">
        <div
          className="col-lg-12 col-md-12 col-sm-12 col-xs-12"
          id="footer-contact-detail"
        >
          <p className="text-justify">
            <a href={Constants.ABOUT_URL}>About</a> |{" "}
            <Link to="/privacy">Privacy Policy</Link> |{" "}
            <Link to="/terms">Terms of Service</Link> |{" "}
            <a href="tel:425-549-0316">425-549-0316</a> |{" "}
            <a href="mailto:info@svpa.us?Subject=Feedback">info@svpa.us</a>
          </p>
          <p className="text-justify">
            Physical Address: 4621 Tolt Avenue, Carnation, WA 98014
            <br />
            U.S. Mail: P.O. Box 1148, Carnation, WA 98014
            <br />© Snoqualmie Valley Preservation Alliance
          </p>
        </div>
        <div className="col-lg-3 col-md-4 col-sm-4 col-xs-12"></div>
      </div>
    </footer>
  );
}
