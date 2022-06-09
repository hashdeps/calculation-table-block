// Template for webpack.config.js in Fable projects
// In most cases, you'll only need to edit the CONFIG object (after dependencies)
// See below if you need better fine-tuning of Webpack options
const path = require("path");
const HtmlWebpackPlugin = require("html-webpack-plugin");
const CopyWebpackPlugin = require("copy-webpack-plugin");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const WebpackAssetsManifest = require("webpack-assets-manifest");
const Dotenv = require("dotenv-webpack");
const { patchGracefulFileSystem } = require("./webpack.common.js");
const { StatsPlugin } = require("./webpack-block-metadata-plugin.cjs");

const packageJsonPath = path.resolve(process.cwd(), "./package.json");
// eslint-disable-next-line import/no-dynamic-require
const { peerDependencies } = require(packageJsonPath);

patchGracefulFileSystem();

// If we're running the webpack-dev-server, assume we're in development mode
var isProduction = !process.argv.find(
  (v) => v.indexOf("webpack-dev-server") !== -1
);

const isDevelopment = !isProduction && process.env.NODE_ENV !== "production";

var CONFIG = {
  // The tags to include the generated JS and CSS will be automatically injected in the HTML template
  // See https://github.com/jantimon/html-webpack-plugin
  indexHtmlTemplate: isProduction ? undefined : "./src/index.html",
  fsharpEntry: isProduction
    ? "./dist/build/Block.fs.js"
    : "./dist/build/Dev.fs.js",
  outputDir: "./dist/out",
  assetsDir: "./public",
  blockInfo: "./block-info",
  devServerPort: 8080,
  // When using webpack-dev-server, you may need to redirect some calls
  // to a external API server. See https://webpack.js.org/configuration/dev-server/#devserver-proxy
  devServerProxy: {
    "/api/*": {
      // assuming the backend is running on port 5000
      target: "http://localhost:5000",
      changeOrigin: true,
    },
  },
  // Use babel-preset-env to generate JS compatible with most-used browsers.
  // More info at https://babeljs.io/docs/en/next/babel-preset-env.html
  babel: {
    presets: ["@babel/preset-env", "@babel/preset-react"],
  },
};

console.log(
  "Bundling for " + (isProduction ? "production" : "development") + "..."
);

// The HtmlWebpackPlugin allows us to use a template for the index.html page
// and automatically injects <script> or <link> tags for generated bundles.
var commonPlugins = [
  new Dotenv({
    path: "./.env",
    silent: false,
    systemvars: true,
  }),
];

module.exports = {
  // In development, bundle styles together with the code so they can also
  // trigger hot reloads. In production, put them in a separate CSS file.
  entry: {
    main: [resolve(CONFIG.fsharpEntry)],
  },
  // Add a hash to the output file name in production
  // to prevent browser caching if code changes
  output: {
    libraryTarget: isProduction ? "commonjs" : undefined,
    path: resolve(CONFIG.outputDir),
    filename: isProduction ? "main.[contenthash].js" : "[name].js",
    clean: true,
  },
  externals: isProduction
    ? Object.fromEntries(Object.keys(peerDependencies).map((key) => [key, key]))
    : undefined,
  mode: isProduction ? "production" : "development",
  devtool: isProduction ? undefined : "eval-source-map",
  optimization: {
    // usedExports: true,
    // moduleIds: "named",
  },
  plugins: isProduction
    ? commonPlugins.concat([
        // new MiniCssExtractPlugin({ filename: "style.[contenthash].css" }),
        new CopyWebpackPlugin({
          patterns: [
            { from: resolve(CONFIG.assetsDir), to: "./public/" },
            { from: resolve(CONFIG.blockInfo) },
          ],
        }),
        new WebpackAssetsManifest({
          output: "manifest.json",
        }),
        new StatsPlugin("dist/out/block-metadata.json"),
      ])
    : commonPlugins.concat([
        new HtmlWebpackPlugin({
          filename: "index.html",
          template: resolve(CONFIG.indexHtmlTemplate),
        }),
        new StatsPlugin("dist/build/block-metadata.json"),
      ]),
  resolve: {
    // See https://github.com/fable-compiler/Fable/issues/1490
    symlinks: false,
    // modules: [resolve("./node_modules")],
    alias: {
      // Some old libraries still use an old specific version of core-js
      // Redirect the imports of these libraries to the newer core-js
      "core-js/es6": "core-js/es",
    },
    extensions: [
      ".js", // Preserving webpack default
      ".jsx", // Preserving webpack default
      ".json", // Preserving webpack default
      ".css", // Preserving webpack default
    ],
  },
  // Configuration for webpack-dev-server
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
    static: {
      directory: resolve(CONFIG.assetsDir),
    },
    port: CONFIG.devServerPort,
    proxy: CONFIG.devServerProxy,
    hot: true,
  },
  // - babel-loader: transforms JS to old syntax (compatible with old browsers)
  // - sass-loaders: transforms SASS/SCSS into JS
  // - file-loader: Moves files referenced in the code (fonts, images) into output folder
  module: {
    rules: [
      {
        test: /\.(js|jsx)$/,
        exclude: function (modulePath) {
          return (
            /node_modules/.test(modulePath) &&
            !/node_modules\/mock-block-dock/.test(modulePath)
          );
        },
        use: {
          loader: "babel-loader",
          options: require("./babelrc.json"),
        },
      },
      {
        test: /\.css$/i,
        use: [
          "style-loader",
          {
            loader: "css-loader",
          },
        ],
      },
      {
        test: /\.s[ac]ss$/i,
        use: ["style-loader", "css-loader", "sass-loader"],
      },
      {
        test: /\.(png|jpg|jpeg|gif|svg|woff|woff2|ttf|eot)(\?.*)?$/,
        use: ["file-loader"],
      },
    ],
  },
};

function resolve(filePath) {
  return path.isAbsolute(filePath) ? filePath : path.join(__dirname, filePath);
}
