#!/usr/bin/env python3
"""Local dev server for Unity WebGL builds with gzip/brotli Content-Encoding.

Usage:
  cd webgl-build
  python3 serve.py 8080

Open http://localhost:8080/ for the branded launch page.
Do not use python3 -m http.server: Unity's compressed files need encoding headers.
"""
import sys
import http.server
import socket
import socketserver
from urllib.parse import urlsplit

PORT = int(sys.argv[1]) if len(sys.argv) > 1 else 8080


class Server(socketserver.TCPServer):
    address_family = socket.AF_INET6
    allow_reuse_address = True

    def server_bind(self):
        self.socket.setsockopt(socket.IPPROTO_IPV6, socket.IPV6_V6ONLY, 0)
        super().server_bind()


class Handler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header("Cache-Control", "no-store")
        path = urlsplit(self.path).path
        if path.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
        elif path.endswith(".br"):
            self.send_header("Content-Encoding", "br")
        super().end_headers()

    def guess_type(self, path):
        # Strip the compression suffix so the underlying content type
        # (js/wasm/data) is still reported correctly to the browser.
        base = path
        if base.endswith(".gz") or base.endswith(".br"):
            base = base.rsplit(".", 1)[0]
        if base.endswith(".wasm"):
            return "application/wasm"
        if base.endswith(".js"):
            return "application/javascript"
        return super().guess_type(base)


if __name__ == "__main__":
    with Server(("", PORT), Handler) as httpd:
        print(f"Serving HTTP on :: port {PORT} (http://localhost:{PORT}/) ...")
        httpd.serve_forever()
