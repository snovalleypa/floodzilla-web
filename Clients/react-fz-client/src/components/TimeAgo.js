import React, { useEffect, useState } from "react";
import moment from "moment";

export default function TimeAgo({ dateTime }) {
  const [timeAgo, setTimeAgo] = useState();
  const [tick, setTick] = useState();

  useEffect(() => {
    if (dateTime) {
      setTimeAgo(moment(dateTime).fromNow());
      const timeoutId = setTimeout(() => {
        setTick(Math.random());
      }, 10000);
      return () => {
        clearTimeout(timeoutId);
      };
    } else {
      setTimeAgo();
    }
  }, [dateTime, tick]);

  if (!timeAgo) return null;
  return <span>{timeAgo}</span>;
}
