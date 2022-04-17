import React, { useState, useEffect } from "react";
var CanvasJS = require("./canvasjs.min");
CanvasJS = CanvasJS.Chart ? CanvasJS : window.CanvasJS;

function CanvasJSChart({ options, chartLoaded, containerStyle }) {
  const [chartContainerId] = useState(
    "canvasjs-react-chart-container-" +
      Math.random()
        .toString()
        .replace("0.", "")
  );
  const [divStyle, setDivStyle] = useState();
  const [chart, setChart] = useState();

  useEffect(() => {
    const chart = new CanvasJS.Chart(chartContainerId, options);
    chart.render();
    if (typeof chartLoaded === "function") {
      chartLoaded(chart);
    }
    setChart(chart);
    return () => {
      chart.destroy();
    };
  }, []);

  useEffect(() => {
    setDivStyle({
      width: "100%",
      position: "relative",
      height: options.height + "px" || "400px",
      ...containerStyle,
    });
    if (chart && options) {
      chart.options = options;
      chart.render();
    }
  }, [options, containerStyle]);

  return <div id={chartContainerId} style={divStyle} />;
}

class CanvasJSChart_old extends React.Component {
  static _cjsContainerId = 0;
  constructor(props) {
    super(props);
    this.options = props.options ? props.options : {};
    this.chartLoaded = props.chartLoaded;
    this.containerProps = props.containerProps
      ? props.containerProps
      : { width: "100%", position: "relative" };
    this.containerProps.height =
      props.containerProps && props.containerProps.height
        ? props.containerProps.height
        : this.options.height
        ? this.options.height + "px"
        : "400px";
    this.chartContainerId =
      "canvasjs-react-chart-container-" + CanvasJSChart._cjsContainerId++;
  }
  componentDidMount() {
    //Create Chart and Render
    this.chart = new CanvasJS.Chart(this.chartContainerId, this.options);
    this.chart.render();
    if (typeof this.chartLoaded === "function") {
      this.chartLoaded(this.chart);
    }

    if (this.props.onRef) this.props.onRef(this.chart);
  }
  shouldComponentUpdate(nextProps, nextState) {
    //Check if Chart-options has changed and determine if component has to be updated
    return !(nextProps.options === this.options);
  }
  componentDidUpdate() {
    //Update Chart Options & Render
    this.chart.options = this.props.options;
    this.chart.render();
  }
  componentWillUnmount() {
    //Destroy chart and remove reference
    this.chart.destroy();
    if (this.props.onRef) this.props.onRef(undefined);
  }
  render() {
    //return React.createElement('div', { id: this.chartContainerId, style: this.containerProps });
    return <div id={this.chartContainerId} style={this.containerProps} />;
  }
}

var CanvasJSReact = {
  CanvasJSChart: CanvasJSChart,
  CanvasJS: CanvasJS,
};

export default CanvasJSReact;
