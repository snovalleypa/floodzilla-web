import React, { useEffect, useState, useContext } from "react";
import Dropdown from "react-bootstrap/Dropdown";
import { Link, useHistory } from "react-router-dom";
import { SessionContext, SessionState } from "./SessionContext";

export default function UserIcon(props) {

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
  const handleLogOut = (e) => {
    e.preventDefault();
    session.logOut();
    history.push('/');
  }

  var icon, greeting, cmd;
  switch (sessionState) {
    case SessionState.NOT_LOGGED_IN:
    default:
      icon = <img src={"/img/round-person-36px-offline.png"} alt="Log In" />;
      greeting = '';
      cmd = <React.Fragment>
              <Dropdown.Item onClick={handleLoginCreateShow}>Login/Create Account</Dropdown.Item>
            </React.Fragment>
      break;
    case SessionState.LOGGING_IN:
      icon = <img src={"/img/round-person-36px.png"} alt="Logging in..." />
      greeting = '';
      cmd = <React.Fragment>
              <Dropdown.Item disabled onClick={session.handleLoginCreateShow}>Login/Create Account</Dropdown.Item>
            </React.Fragment>
      break;
    case SessionState.LOGGED_IN:
      icon = <img src={"/img/round-person-36px-loggedin.png"} alt="Logged in..." />;
      greeting = <><Dropdown.Item>Welcome, {session.getFirstName()} {session.getLastName()} ({session.getUsername()})!</Dropdown.Item><Dropdown.Divider /></>;
      cmd = <React.Fragment>
              <Dropdown.Item as="div"><Link to='/user/profile'>Edit Profile</Link></Dropdown.Item>
              <Dropdown.Item as="div"><Link to='/user/alerts'>Alert Settings</Link></Dropdown.Item>
              <Dropdown.Item onClick={handleLogOut}>Log Out</Dropdown.Item>
            </React.Fragment>
      break;
  }

  return (
    <div>
      <Dropdown>
        <Dropdown.Toggle variant="link" className="user-icon">{icon}</Dropdown.Toggle>
        <Dropdown.Menu alignRight>
          {greeting}
          {cmd}
        </Dropdown.Menu>
      </Dropdown>
    </div>
  );
}

