import React from "react";

export default function LoginField({label, labelFor, ctrl}) {
  return (
    <div className="row login-form-row">
      <div className="col-sm-4 my-auto">
        <label htmlFor={labelFor} className="text-secondary login-label text-sm-right text-left">{label}</label>
      </div>
      <div className="col-sm-8">
        {ctrl}
      </div>
    </div>
  );
}
