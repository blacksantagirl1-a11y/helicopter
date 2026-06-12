from __future__ import annotations

import os
import runpy
import sys
import tempfile
from pathlib import Path


workspace_tmp = Path(__file__).resolve().parents[1] / ".tmp_docx_render"
workspace_tmp.mkdir(exist_ok=True)
os.environ["TMP"] = str(workspace_tmp)
os.environ["TEMP"] = str(workspace_tmp)
os.environ["TMPDIR"] = str(workspace_tmp)
tempfile.tempdir = str(workspace_tmp)

render_script = Path(__file__).with_name("render_docx.py")
sys.argv[0] = str(render_script)
runpy.run_path(str(render_script), run_name="__main__")
