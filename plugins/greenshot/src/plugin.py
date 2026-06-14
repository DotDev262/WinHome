import os
import shutil
import tempfile
from configparser import ConfigParser
from typing import Any, Dict


class GreenshotPlugin:
    """Configuration provider plugin for managing Greenshot screenshot tool settings."""

    def __init__(self, args: Dict[str, Any]):
        self.args = args
        self.request_id = args.get("requestId", "unknown")
        self.settings = args.get("settings", {})
        
        # Resolve target Windows AppData environment path
        app_data = os.environ.get("APPDATA", os.path.expanduser("~\\AppData\\Roaming"))
        self.config_dir = os.path.join(app_data, "Greenshot")
        self.config_path = os.path.join(self.config_dir, "Greenshot.ini")

    def check_installed(self) -> bool:
        """Detects if Greenshot is installed by checking directory presence or PATH execution context."""
        if os.path.exists(self.config_dir):
            return True
        return shutil.which("Greenshot.exe") is not None

    def apply(self) -> Dict[str, Any]:
        """Reads existing config file, deep-merges nested properties, and handles atomic writes with dryRun support."""
        dry_run = self.args.get("dryRun", False)
        
        # Initialize parser with case-preserving configurations
        config = ConfigParser()
        config.optionxform = str  # type: ignore

        # Load existing configuration file parameters if available
        if os.path.exists(self.config_path):
            config.read(self.config_path, encoding="utf-8")

        # Process and parse incoming settings layout parameters
        for compound_key, value in self.settings.items():
            if "\\" in compound_key:
                section, key = compound_key.split("\\", 1)
            else:
                section, key = "General", compound_key

            if not config.has_section(section):
                config.add_section(section)
            
            # Format values cleanly to strings matching standard INI configuration expectations
            if isinstance(value, bool):
                config.set(section, key, "True" if value else "False")
            else:
                config.set(section, key, str(value))

        if dry_run:
            return {"requestId": self.request_id, "status": "success", "changed": True, "dryRun": True}

        # Guarantee destination directory container blocks exist natively
        if not os.path.exists(self.config_dir):
            os.makedirs(self.config_dir, exist_ok=True)

        # Execute safe atomic file creation pass using tempfile routines
        fd, temp_path = tempfile.mkstemp(dir=self.config_dir, text=True)
        try:
            with os.fdopen(fd, "w", encoding="utf-8") as f:
                config.write(f, space_around_delimiters=False)
            
            # Enforce trailing POSIX newline constraint checks natively on the output file asset
            with open(temp_path, "a", encoding="utf-8") as f:
                f.write("\n")
                
            os.replace(temp_path, self.config_path)
        except Exception as e:
            if os.path.exists(temp_path):
                os.remove(temp_path)
            raise e

        return {"requestId": self.request_id, "status": "success", "changed": True}

