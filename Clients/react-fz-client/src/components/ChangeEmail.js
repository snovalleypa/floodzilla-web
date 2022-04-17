import React, { useContext, useState } from "react";
import Alert from "react-bootstrap/Alert";
import Button from "react-bootstrap/Button";
import Card from "react-bootstrap/Card";
import Form from "react-bootstrap/Form";
import Image from "react-bootstrap/Image";
import { Link } from "react-router-dom";
import { SessionContext } from "./SessionContext";

export default function ChangeEmail() {

  const session = useContext(SessionContext);

  const [ email, setEmail ] = useState('');
  const [ isLoading, setIsLoading ] = useState(false);
  const [ success, setSuccess ] = useState('');
  const [ error, setError ] = useState('');

  const handleEmailChange = (event) => {
    setEmail(event.target.value);
  }

  const handleSubmit = (event) => {

    event.preventDefault();
    setError('');
    setIsLoading(true);

    session.changeEmail(email, onSuccess, onFail);
  }

  const onSuccess = () => {
    setIsLoading(false);
    setSuccess('Done!');
  }

  const onFail = (errorMessage) => {
    setIsLoading(false);
    setError(errorMessage);
  }

  return (
  <div>
    <Card>
      <Card.Body>
        <Card>
          <Form onSubmit={handleSubmit}>
            <Card.Header>Change your email address</Card.Header>
            <Card.Body>
              {error &&
              <div className="form-group">
                <Alert variant="danger">{error}</Alert>
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
                <label className="text-info">New Email Address:</label><br />
                <input name="email" className="form-control" value={email} onChange={handleEmailChange} />
              </div>
            </Card.Body>
            <Card.Footer>
              {isLoading
              ? <Image className="login-loading" src="/img/loading.gif" />
              : <Button type="submit" variant="primary">Update</Button>
              }
            </Card.Footer>
          </Form>
        </Card>
      </Card.Body>
    </Card>
  </div>
  );
}