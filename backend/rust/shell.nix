with import <nixpkgs> { };
let
  rust-overlay = (import (builtins.fetchTarball "https://github.com/oxalica/rust-overlay/archive/master.tar.gz"));
  pkgs = (import <nixpkgs> {
    overlays = [ rust-overlay ];
  });
in
mkShell {
  buildInputs = [
    (pkgs.rust-bin.stable.latest.default.override {
      extensions = [ "rust-src" ];
    })
  ];
  nativeBuildInputs = [
    llvmPackages.libclang
    llvmPackages.libcxxClang
    clang
    pkg-config
    rust-analyzer
    cargo-deny
    cargo-watch
  ];
  PKG_CONFIG_PATH = "${openssl.dev}/lib/pkgconfig";
  LIBCLANG_PATH = "${llvmPackages.libclang.lib}/lib";
  # BINDGEN_EXTRA_CLANG_ARGS = "-isystem ${llvmPackages.libclang.lib}/lib/clang/${lib.getVersion clang}/include";
  shellHook = ''
    export BINDGEN_EXTRA_CLANG_ARGS="$(< ${stdenv.cc}/nix-support/libc-crt1-cflags) \
          $(< ${stdenv.cc}/nix-support/libc-cflags) \
          $(< ${stdenv.cc}/nix-support/cc-cflags) \
          $(< ${stdenv.cc}/nix-support/libcxx-cxxflags) \
          ${
            lib.optionalString stdenv.cc.isClang
            "-idirafter ${stdenv.cc.cc}/lib/clang/${
              lib.getVersion stdenv.cc.cc
            }/include"
          } \
          ${
            lib.optionalString stdenv.cc.isGNU
            "-isystem ${stdenv.cc.cc}/include/c++/${
              lib.getVersion stdenv.cc.cc
            } -isystem ${stdenv.cc.cc}/include/c++/${
              lib.getVersion stdenv.cc.cc
            }/${stdenv.hostPlatform.config} -idirafter ${stdenv.cc.cc}/lib/gcc/${stdenv.hostPlatform.config}/${
              lib.getVersion stdenv.cc.cc
            }/include"
          } \
        "
  '';
}
