import json
import http.client
import importlib.util
from functools import partial
from pathlib import Path
import re
import sys
import threading
import unittest
from unittest.mock import patch


ROOT = Path(__file__).resolve().parents[1]


class WebGLHostingTests(unittest.TestCase):
    def test_launch_page_matches_build_location_and_compression(self):
        page = (ROOT / "webgl-build/index.html").read_text()
        build_script = (ROOT / "Assets/Editor/WebGLBuildScript.cs").read_text()
        output = re.search(r'OutputPath = "([^"]+)"', build_script).group(1)
        build_name = Path(output).name
        build_url = re.search(r'var buildUrl\s*=\s*"([^"]+)"', page).group(1)

        self.assertEqual(Path("webgl-build") / build_url, Path(output) / "Build")
        for suffix in ("loader.js", "data.gz", "framework.js.gz", "wasm.gz"):
            with self.subTest(suffix=suffix):
                self.assertIn(f'"/{build_name}.{suffix}"', page)
        self.assertIn('streamingAssetsUrl: "loki/StreamingAssets"', page)

    def test_all_unity_binary_urls_share_a_build_version(self):
        page = (ROOT / "webgl-build/index.html").read_text()
        self.assertRegex(page, r'var buildVersion\s*=\s*"[^"]+"')
        for suffix in ("loader.js", "data.gz", "framework.js.gz", "wasm.gz"):
            self.assertRegex(page,
                rf'"/loki\.{re.escape(suffix)}"\s*\+\s*"\?v="\s*\+\s*buildVersion')
        build_script = (ROOT / "Assets/Editor/WebGLBuildScript.cs").read_text()
        self.assertIn("UpdateLaunchVersion", build_script)

    def test_firebase_declares_types_for_gzip_unity_files(self):
        config = json.loads((ROOT / "firebase.json").read_text())
        rules = config["hosting"]["headers"]
        for suffix, content_type in (
            ("data", "application/octet-stream"),
            ("wasm", "application/wasm"),
            ("js", "application/javascript"),
        ):
            with self.subTest(suffix=suffix):
                headers = {
                    header["key"]: header["value"]
                    for rule in rules
                    if rule["source"] == f"**/*.{suffix}.gz"
                    for header in rule["headers"]
                }
                self.assertEqual(headers.get("Content-Type"), content_type)
                self.assertEqual(headers.get("Content-Encoding"), "gzip")

    def test_deploy_checks_the_actual_build_directory(self):
        script = (ROOT / "deploy.sh").read_text()
        self.assertIn('BUILD_OUTPUT="${PROJECT_DIR}/webgl-build/loki/Build"', script)

    def test_firebase_revalidates_build_files_with_stable_names(self):
        config = json.loads((ROOT / "firebase.json").read_text())
        rules = config["hosting"]["headers"]
        for source in ("**/*.gz", "**/*.br", "**/*.loader.js"):
            with self.subTest(source=source):
                values = [
                    header["value"]
                    for rule in rules
                    if rule["source"] == source
                    for header in rule["headers"]
                    if header["key"] == "Cache-Control"
                ]
                self.assertEqual(["no-cache"], values,
                    "Unhashed Unity filenames must revalidate after a rebuild.")

    def test_local_server_accepts_ipv4_and_ipv6(self):
        spec = importlib.util.spec_from_file_location("serve", ROOT / "webgl-build/serve.py")
        serve = importlib.util.module_from_spec(spec)
        with patch.object(sys, "argv", ["serve.py"]):
            spec.loader.exec_module(serve)
        handler = partial(serve.Handler, directory=str(ROOT / "webgl-build"))
        with serve.Server(("", 0), handler) as server:
            thread = threading.Thread(target=server.serve_forever)
            thread.start()
            try:
                for host in ("127.0.0.1", "::1"):
                    with self.subTest(host=host):
                        connection = http.client.HTTPConnection(
                            host, server.server_address[1], timeout=3
                        )
                        try:
                            connection.request("GET", "/")
                            response = connection.getresponse()
                            self.assertEqual(response.status, 200)
                            self.assertEqual("no-store", response.getheader("Cache-Control"),
                                "Local builds must not mix a cached framework with a new wasm.")
                            self.assertIn(b"loki/Build", response.read())
                            connection.request("HEAD", "/loki/Build/loki.framework.js.gz?v=test")
                            asset = connection.getresponse()
                            self.assertEqual(200, asset.status)
                            self.assertEqual("gzip", asset.getheader("Content-Encoding"))
                            self.assertEqual("application/javascript", asset.getheader("Content-Type"))
                            asset.read()
                        except OSError as error:
                            self.fail(f"Game server is unreachable on {host}: {error}")
                        finally:
                            connection.close()
            finally:
                server.shutdown()
                thread.join()


if __name__ == "__main__":
    unittest.main()
