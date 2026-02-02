import sys
import json

def main():
    try:
        raw_input = sys.stdin.read()
        request = json.loads(raw_input)
        
        response = {
            "requestId": request.get("requestId"),
            "success": True,
            "changed": True,
            "data": {
                "echo": request.get("args", {}).get("message", "no message"),
                "python_version": sys.version
            }
        }
        
        print(json.dumps(response))
    except Exception as e:
        print(json.dumps({"success": False, "error": str(e)}))

if __name__ == "__main__":
    main()
