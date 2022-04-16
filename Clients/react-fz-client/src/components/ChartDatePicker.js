import React from "react";
import moment from "moment-timezone";
import { DateRangePicker } from "react-dates";

//$ TODO: change moment() -> debug.getNow()?

// Memo to myself: it appears that because chartRange.inputStartDate/inputEndDate are in region
// time, the dates returned by this control are also in region time.

export default function ChartDatePicker({
  chartRange,
  isMobile,
  focusedInput,
  onDatePickerChange,
  onCalendarFocus,
}) {
  // css overrides in DatePickerOverrides
  return (
    <div className="date" id="chart-calendar">
      <DateRangePicker
        small
        startDate={chartRange.inputStartDate}
        startDateId="startDateId"
        endDate={chartRange.inputEndDate}
        endDateId="endDateId"
        onDatesChange={({ startDate, endDate }) => {
            onDatePickerChange({ startDate, endDate, chartRange })
          }
        }
        focusedInput={focusedInput}
        onFocusChange={input => {
          onCalendarFocus(input, isMobile);
        }}
        numberOfMonths={1}
        minimumNights={0}
        hideKeyboardShortcutsPanel
        noBorder={isMobile}
        showDefaultInputIcon={isMobile}
        isOutsideRange={day =>
          day.startOf("day") > moment().tz(chartRange.timeZone)
        }
        openDirection={isMobile ? "up" : "down"}
      />
    </div>
  );
}
