import React, { useEffect, useState, useContext } from "react";
import Dropdown from "react-bootstrap/Dropdown";
import DropdownButton from "react-bootstrap/DropdownButton";
import Navbar from "react-bootstrap/Navbar";
import "../style/Dashboard.css";
import "../style/DashboardMenu.css";
import Constants from "../constants";
import { Link, useHistory, useLocation } from "react-router-dom";
import UserIcon from "./UserIcon";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faBars } from '@fortawesome/free-solid-svg-icons';
import FakeNowControl from "./FakeNowControl";
import { DebugContext } from "./DebugContext";
import { SessionContext, SessionState } from "./SessionContext";

export default function Header({ filter, onSearchChange }) {
  const debug = useContext(DebugContext);
  const location = useLocation();
  const session = useContext(SessionContext);
  const history = useHistory();

  const [didFirstLogin, setDidFirstLogin] = useState(false);

  // if we've got a saved token, use it to verify we're logged in.
  useEffect(() => {
    if (!didFirstLogin) {
      setDidFirstLogin(true);
      session.reauthenticate();
    }
  }, [didFirstLogin, session]);

  const sessionState = session.sessionState;
  const handleLoginCreateShow = (e) => {
    e.preventDefault();
    session.showLoginCreateModal({});
  }
  const handleNliGetAlerts = (e) => {
    e.preventDefault();
    session.showLoginCreateModal({destPath: "/user/alerts"});
  }
  const handleLogOut = (e) => {
    e.preventDefault();
    session.logOut();
    history.push('/');
  }

  return (
    <div className="navbar-wrapper fixed-top">
      <Navbar bg="default" variant="default">
        <div className="container-fluid">
          <div className="navbar-header" style={{width:"100%"}} className="d-flex justify-content-between">
            {location.pathname === "/" && <div className="d-none d-lg-block menu-search">
              <div style={{ paddingTop: "15px" }}>
                <div className="has-feedback right-menu-style" id="div_search">
                  <input
                    type="text"
                    id="search"
                    className="form-control"
                    onChange={onSearchChange}
                    placeholder="search road, place..."
                  />
                </div>
              </div>
            </div>}

            <Navbar.Brand as="div">
              <div className="mr-auto"
                style={{
                  display: "flex",
                  width: 280,
                  justifyContent: "space-between",
                }}
              >
                <Link to="/" title="home">
                  <img
                    src="/img/floodzilla_logo192.png"
                    className="logo"
                    alt="Floodzilla Logo"
                  />
                </Link>
                <div style={{ marginTop: -5 }}>
                  <Link to="/">
                    <div className="logo-text">{debug.isDebugMode ? "DEBUG MODE!" : Constants.SITE_TITLE}</div>
                  </Link>
                  <a
                    href={Constants.ABOUT_URL}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="logo-text"
                    style={{ fontSize: "0.7em" }}
                  >
                    {Constants.SITE_SUBTITLE}
                  </a>
                </div>
              </div>
            </Navbar.Brand>
          <ul className="d-none d-lg-flex menu navbar-item navbar-nav">
            <HeaderTab isActive={location.pathname === '/' && filter.active} text="Active" linkTo="/" />
            <HeaderTab isActive={location.pathname === '/forecast'} text="Forecast" linkTo="/forecast" />
            {sessionState === SessionState.NOT_LOGGED_IN && <>
              <HeaderTab isActive={location.pathname === '/user/alerts'} text="Get Alerts" onClick={handleNliGetAlerts} />
            </>}
            {sessionState === SessionState.LOGGING_IN && <>
              <HeaderTab isDisabled="true" isActive={location.pathname === '/user/alerts'} text="Get Alerts" linkTo="/user/alerts" />
            </>}
            {sessionState === SessionState.LOGGED_IN && <>
              <HeaderTab isActive={location.pathname === '/user/alerts'} text="Get Alerts" linkTo="/user/alerts" />
            </>}
            {debug.isDebugMode && <li>
              <FakeNowControl />
            </li>}
          </ul>

          <div className="d-flex menu-wrapper justify-content-end">
            <div className="d-flex flex-column justify-content-center">
              <Dropdown className="navbar-item d-flex">
                <Dropdown.Toggle
                   id="menuToggle"
                   className="menu-toggler"
                   style={{  }}>
                  <span className="sr-only">Toggle navigation</span>
                  <FontAwesomeIcon style={{color:"white"}} icon={faBars} />
                </Dropdown.Toggle>
                <Dropdown.Menu align="right">
                  <Dropdown.Item className="d-block d-lg-none menu-item" href="/">Active Gages</Dropdown.Item>
                  <Dropdown.Item className="d-block d-lg-none menu-item" href="/forecast">Forecast</Dropdown.Item>
                  {sessionState === SessionState.NOT_LOGGED_IN && <>
                    <Dropdown.Item className="d-block d-lg-none menu-item" onClick={handleNliGetAlerts}>Get Alerts</Dropdown.Item>
                  </>}
                  {sessionState === SessionState.LOGGING_IN && <>
                    <Dropdown.Item className="d-block d-lg-none menu-item" href="#">...</Dropdown.Item>
                  </>}
                  {sessionState === SessionState.LOGGED_IN && <>
                    <Dropdown.Item className="d-block d-lg-none menu-item" href="/user/alerts">Get Alerts</Dropdown.Item>
                  </>}
                  <Dropdown.Item className="menu-item" href="/?showAll=true">All Gages</Dropdown.Item>
                  {sessionState === SessionState.NOT_LOGGED_IN && <>
                    <Dropdown.Item className="menu-item" onClick={handleLoginCreateShow}>Login/Create Account</Dropdown.Item>
                  </>}
                  {sessionState === SessionState.LOGGING_IN && <>
                    <Dropdown.Item className="menu-item" href="#">...</Dropdown.Item>
                  </>}
                  {sessionState === SessionState.LOGGED_IN && <>
                    <Dropdown.Item className="menu-item" href="/user/profile">Edit Profile</Dropdown.Item>
                    <Dropdown.Item className="menu-item" onClick={handleLogOut}>Logout</Dropdown.Item>
                  </>}
                </Dropdown.Menu>
              </Dropdown>
            </div>
          </div>
          </div>
        </div>
      </Navbar>
    </div>
  );
}

function HeaderTab({isActive, isDisabled, text, linkTo, onClick}) {
  return (
    <li
      className={isDisabled ? "header-disabled" : isActive ? "active" : ""}
      style={{
        cursor: "pointer",
        display: "block",
      }}
    >
      <Link
        disable={isDisabled}
        className=""
        data-toggle="collapse"
        data-target=".in"
        onClick={onClick}
        to={linkTo || "#"}
      >
        {text}
      </Link>
    </li>
  );
}
