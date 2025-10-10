const PROXY_CONFIG = [
  {
    context: [
      "/api",
      "/weatherforecast"
    ],
    target: "http://localhost:5171",
    secure: false,
    changeOrigin: true
  }
];

module.exports = PROXY_CONFIG;
