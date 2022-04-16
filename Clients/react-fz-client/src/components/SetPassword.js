import React, { useContext, useEffect, useState } from "react";
import Alert from "react-bootstrap/Alert";
import Button from "react-bootstrap/Button";
import Card from "react-bootstrap/Card";
import Form from "react-bootstrap/Form";
import Image from "react-bootstrap/Image";
import { Link, useHistory, useLocation } from "react-router-dom";
import { SessionContext } from "./SessionContext";
import LoginField from "./LoginField";
import Constants from "../constants";

export default function SetPassword() {

  const session = useContext(SessionContext);

  const [ showOld, setShowOld ] = useState(false);
  const [ title, setTitle ] = useState('');
  const [ oldPassword, setOldPassword ] = useState('');
  const [ newPassword, setNewPassword ] = useState('');
  const [ confirmPassword, setConfirmPassword ] = useState('');
  const [ passwordError, setPasswordError ] = useState('');
  const [ intro, setIntro ] = useState(null);
  const [ isLoading, setIsLoading ] = useState(false);
  const [ success, setSuccess ] = useState('');
  const [ error, setError ] = useState('');
  const [ isFormValid, setIsFormValid ] = useState(false);

  const history = useHistory();
  const location = useLocation();
  function useQuery() {
    return new URLSearchParams(location.search);
  }
  const query = useQuery();
  const userId = query.get('userId');
  const code = query.get('code');

  const handleOldPasswordChange = (event) => {
    setOldPassword(event.target.value);
    validateForm(true);
  }
  const handleNewPasswordChange = (event) => {
    setNewPassword(event.target.value);
    validateForm(true);
  }
  const handleConfirmPasswordChange = (event) => {
    setConfirmPassword(event.target.value);
    validateForm(true);
  }
  const handleNewPasswordBlur = (event) => {
    validateForm(validatePassword());
  }
  const handleConfirmPasswordBlur = (event) => {
    validateForm(validatePassword());
  }

  const validatePassword = () => {
    if (newPassword !== confirmPassword) {
      setPasswordError('Passwords do not match');
      return false;
    } else if (newPassword.length < Constants.PASSWORD_MIN_LENGTH) {
      setPasswordError('Password must be at least ' + Constants.PASSWORD_MIN_LENGTH + ' characters.');
      return false;
    } else {
      setPasswordError('');
      return true;
    }
  }

  const validateForm = (passwordIsValid) => {
    setError('');
    var valid = true;
    if (!passwordIsValid ||
        confirmPassword.length === 0 ||
        newPassword.length === 0 ||
        (showOld && (oldPassword.length === 0)) ||
        newPassword !== confirmPassword) {
      valid = false;
      }
    setIsFormValid(valid);
  }

  const handleSubmit = (event) => {

    event.preventDefault();
    setError('');
    setSuccess('');
    setIsLoading(true);

    switch (location.pathname) {
      default:
      case '/user/setpassword':
        session.setPassword(oldPassword, newPassword, onSuccess, onFail);
        break;
      case '/user/createpassword':
        session.createPassword(newPassword, onCreatePasswordSuccess, onFail);
        break;
      case '/user/resetpassword':
        session.resetPassword(userId, code, newPassword, onSuccess, onFail);
        break;
    }
  }

  const onSuccess = () => {
    setIsLoading(false);
    setSuccess('Done!');
  }

  const onCreatePasswordSuccess = () => {
    setIsLoading(false);
    history.push('/user/changeemail');
  }

  const onFail = (errorMessage) => {
    setIsLoading(false);
    setError(errorMessage);
  }

  useEffect(() => {

    setShowOld(false);
    switch (location.pathname) {
      default:
      case '/user/setpassword':
        setShowOld(true);
        setTitle('Change your password');
        break;
      case '/user/resetpassword':
        setTitle('Reset your password');
        if (!code || !userId) {
          //$ TODO: what do we want to do here?
          history.push('/');
        }
        break;
      case '/user/createpassword':
        setTitle('Choose a password');
        setIntro('In order to change your email address, you must first create a password for your Floodzilla account.');
        break;
    }

  }, [code, userId, history, location]);

  return (
  <div className="profile-page">
    <Card className="profile-card">
      <Card.Header className="text-center">{title}</Card.Header>
      <Card.Body>
        <div className="container">
          <Form onSubmit={handleSubmit}>
            {intro &&
              <div className="login-form-result-row">
                {intro}
              </div>
            }
            {success &&
              <div className="form-group">
                <Alert variant="success">{success}<br />
                <Link to='/'>Click here to proceed to Floodzilla</Link>
                </Alert>
              </div>
            }
            <div className="form-group">
              {showOld && <LoginField
                label={<>Current Password:</>}
                ctrl={<input type="password" name="oldPassword" className="form-control login-form-control" value={oldPassword} onChange={handleOldPasswordChange} />}
              />}
            <LoginField
               label={<>New Password:</>}
               ctrl={<input type="password" name="newPassword" className="form-control login-form-control" value={newPassword} onChange={handleNewPasswordChange} onBlur={handleNewPasswordBlur} />}
              />
            <LoginField
               label={<>Confirm Password:</>}
               ctrl={<input type="password" name="confirmPassword" className="form-control login-form-control" value={confirmPassword} onChange={handleConfirmPasswordChange} onBlur={handleConfirmPasswordBlur} />}
              />
            </div>
            {passwordError &&
              <div className="login-form-error-row">
                {passwordError}
              </div>
            }
            {error &&
              <div className="login-form-error-row">
                {error}
              </div>
            }
            <div className="login-form-button-group justify-content-center">
              {isLoading
                  ? <Image className="login-loading" src="/img/loading.gif" />
                  : <Button disabled={!isFormValid} type="submit" variant="primary">Update</Button>
                  }
            </div>
          </Form>
        </div>
      </Card.Body>
    </Card>
   </div>
  );
}