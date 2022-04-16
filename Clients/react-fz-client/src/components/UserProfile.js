import React, { useContext, useEffect, useState } from "react";
import Button from "react-bootstrap/Button";
import Card from "react-bootstrap/Card";
import Form from "react-bootstrap/Form";
import Image from "react-bootstrap/Image";
import { Link } from "react-router-dom";
import { SessionContext } from "./SessionContext";
import "../style/Profile.css";
import LoginField from "./LoginField";
import RequireLogin from "./RequireLogin";

export default function UserProfile() {

  const session = useContext(SessionContext);

  const [ firstName, setFirstName ] = useState('');
  const [ lastName, setLastName ] = useState('');
  const [ email, setEmail ] = useState('');
  const [ phone, setPhone ] = useState('');
  const [ isLoading, setIsLoading ] = useState(false);
  const [ error, setError ] = useState('');
  const [ result, setResult ] = useState('');
  const [ isFormValid, setIsFormValid ] = useState(false);

  const handleFirstNameChange = (event) => {
    const s = stripInvalidChars(event.target.value);
    setFirstName(s);
    validateForm(s, lastName, email);
  }
  const handleLastNameChange = (event) => {
    const s = stripInvalidChars(event.target.value);
    setLastName(s);
    validateForm(firstName, s, email);
  }
  const handleEmailChange = (event) => {
    const s = stripInvalidChars(event.target.value);
    setEmail(s);
    validateForm(firstName, lastName, s);
  }

  const handlePhoneShow = (e) => {
    e.preventDefault();
    session.showPhoneModal();
  }

  const sanitizeInput = (s) => {
    return s.replace( /(<([^>]+)>)/ig, '').trim();
  }

  const stripInvalidChars = (s) => {
    return s.replace( /[<&]/ig, '');
  }

  const validateForm = (first, last, em) => {
    var valid = true;
    if (sanitizeInput(first).length === 0 ||
        sanitizeInput(last).length === 0 ||
        sanitizeInput(em).length === 0)
    {
      valid = false;
    }
    setIsFormValid(valid);
  }

  useEffect(() => {
    setFirstName(session.getFirstName());
    setLastName(session.getLastName());
    setEmail(session.getUsername());
    setPhone(session.getPhone());
  }, [session]);

  const onUpdateProfileSuccess = () => {
    setIsLoading(false);
    setError('');
    setResult('Updated.');
  }

  const onUpdateProfileFail = (errorMessage) => {
    setIsLoading(false);
    setError(errorMessage);
    setResult('');
  }

  const handleSubmit = (event) => {
    event.preventDefault();
    setError('');
    setResult('');
    setIsLoading(true);
    session.updateProfile(sanitizeInput(firstName), sanitizeInput(lastName), sanitizeInput(email), onUpdateProfileSuccess, onUpdateProfileFail);
  }

  return (
  <div className="profile-page">
    <RequireLogin />
    <Card className="profile-card">
      <Card.Header className="text-center">Edit Profile</Card.Header>
      <Card.Body>
        <div className="container">
          <Form onSubmit={handleSubmit}>
            <div className="form-group">
              {session.getHasPassword() ?
              <>
                <LoginField
                   label={<>First Name:</>}
                   ctrl={<input spellCheck="false" type="text" name="firstName" className="form-control login-form-control" value={firstName} onChange={handleFirstNameChange} />}
                 />
                <LoginField
                   label={<>Last Name:</>}
                   ctrl={<input spellCheck="false" type="text" name="lastName" className="form-control login-form-control" value={lastName} onChange={handleLastNameChange} />}
                 />
                <LoginField
                   label={<>Email:</>}
                   ctrl={<input spellCheck="false" type="text" name="email" className="form-control login-form-control" value={email} onChange={handleEmailChange} />}
                 />
              </>
              :
              <>
                <LoginField
                   label={<></>}
                   ctrl={<label className="text-secondary login-label">Linked to {session.getLoginProviderName()} account:</label>}
                 />
                <LoginField
                   label={<>First Name:</>}
                   ctrl={<label className="text-secondary login-label">{firstName}</label>}
                 />
                <LoginField
                   label={<>Last Name:</>}
                   ctrl={<label className="text-secondary login-label">{lastName}</label>}
                 />
                <LoginField
                   label={<>Email:</>}
                   ctrl={<label className="text-secondary login-label">{email}</label>}
                 />
              </>
              }
              {phone
              ? <LoginField
                   label={<>Phone:</>}
                   ctrl={<div className="login-field-text">{phone + '  '} <Link to="/" onClick={handlePhoneShow}>(Update...)</Link></div>}
                />
              : <LoginField
                   label={<>Phone:</>}
                   ctrl={<div className="login-field-text"><Link to="/" onClick={handlePhoneShow}>Enter phone number for SMS alerts</Link></div>}
                />
              }
              <LoginField
                 label={<>Password:</>}
                 ctrl={<div className="login-field-text"><Link to="/user/setpassword">Change Password</Link></div>}
              />
              <div className="col my-auto">
                
              </div>
            </div>
            {error &&
              <div className="login-form-error-row">
                {error}
              </div>
            }
            {result &&
              <div className="login-form-result-row">
                {result}
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
