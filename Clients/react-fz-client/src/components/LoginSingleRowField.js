import React from "react";

export default function LoginSingleRowField({label, labelFor, ctrl}) {
  return (
    <>
      <div className="row login-form-row d-lg-none">
        <div className="col-sm-12 my-auto">
          <label htmlFor={labelFor} className="text-secondary login-singlerow-label text-sm-right text-left">{label}</label>
          {ctrl}
        </div>
      </div>
      <div className="row login-form-row d-none d-lg-flex">
        <div className="col-sm-4 my-auto">
          <label htmlFor={labelFor} className="text-secondary login-label text-sm-right text-left">{label}</label>
        </div>
        <div className="col-sm-8">
          {ctrl}
        </div>
      </div>
    </>
  );
}
