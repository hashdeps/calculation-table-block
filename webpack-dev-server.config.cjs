const webpack = require("webpack");
const path = require("path");
const HtmlWebpackPlugin = require("html-webpack-plugin");
const CopyWebpackPlugin = require("copy-webpack-plugin");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const ReactRefreshWebpackPlugin = require("@pmmmwh/react-refresh-webpack-plugin");
const Dotenv = require("dotenv-webpack");
const { patchGracefulFileSystem } = require("./webpack.common.js");
patchGracefulFileSystem();

/** @type import("webpack").Configuration */
module.exports = {
  devtool: "eval-cheap-module-source-map",
  entry: "./src/dev.js",
  plugins: [
    new HtmlWebpackPlugin({
      filename: "index.html",
      template: path.resolve(__dirname, "index.html"),
    }),
    new webpack.EnvironmentPlugin({
      "process.env.NODE_ENV": "development",
    }),
    new webpack.HotModuleReplacementPlugin(),
  ],
  module: {
    rules: [
      {
        test: /\.(js|jsx)$/,
        exclude: /node_modules/,
        use: {
          loader: "babel-loader",
          options: CONFIG.babel,
        },
      },
      {
        test: /\.(sass|scss|css)$/,
        use: [
          isProduction ? MiniCssExtractPlugin.loader : "style-loader",
          {
            loader: "css-loader",
          },
          {
            loader: "sass-loader",
            options: { implementation: require("sass") },
          },
        ],
      },
      {
        test: /\.(png|jpg|jpeg|gif|svg|woff|woff2|ttf|eot)(\?.*)?$/,
        use: ["file-loader"],
      },
    ],
  },
  mode: "development",
  target: "web",
  devServer: {
    headers: {
      "Access-Control-Allow-Origin": "*",
      "Access-Control-Allow-Methods": "GET, POST, PUT, DELETE, PATCH, OPTIONS",
      "Access-Control-Allow-Headers": [
        "X-Requested-With, content-type, Authorization",
        "X-Requested-With",
        "content-type",
        "Authorization",
        "sentry-trace",
      ],
    },
    hot: true,
    open: process.env.BROWSER !== "none",
    static: {
      directory: __dirname,
    },
  },
  optimization: {
    moduleIds: "named",
  },
  resolve: {
    extensions: [
      ".ts", // Add typescript support
      ".tsx", // Add typescript + react support
      ".js", // Preserving webpack default
      ".jsx", // Preserving webpack default
      ".json", // Preserving webpack default
      ".css", // Preserving webpack default
    ],
  },
};
