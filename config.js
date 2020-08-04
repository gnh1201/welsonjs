exports.config = {
    appName: "welsonjs",
    development: false,
    shadowsocks: {
        host: "158.247.196.146",
        port: 8388,
        password: "korea82",
        cipher: "chacha20-ietf-poly1305"
    },
    webapp: {
        baseURL: "http://158.247.196.146/"
    }
};
