from pathlib import Path
import hashlib

SOURCE = "Assets/Game/Scripts/Simulation/Mathematics/SharedProductionDeliveryKernel.cs"
OUTPUT = "SharedProductionDeliveryKernel.cs"


def generate(repo, output):
    repo = Path(repo)
    output = Path(output)
    source_path = repo / SOURCE
    raw = source_path.read_bytes()
    text = raw.decode("utf-8").replace("\r\n", "\n")
    signature = "public static UnityEngine.Vector3 ProductionTransportLaneOffset(bool hasCameraForward,UnityEngine.Vector3 cameraForward,int laneIndex,int maxConcurrent)"
    if text.count(signature) != 1:
        raise ValueError("Unique current lane policy signature required")
    if text.count("UnityEngine.Vector3") != 5 or text.count("UnityEngine.Mathf") != 1:
        raise ValueError("Unsupported UnityEngine vector/math reference shape")
    body = text.replace("UnityEngine.Vector3", "ActualVector3").replace("UnityEngine.Mathf", "ActualMathf")
    if "UnityEngine.Vector3" in body or "UnityEngine.Mathf" in body:
        raise ValueError("Unbound UnityEngine type in generated alias source")
    transformed = (
        "extern alias ActualCore;\n"
        "using ActualVector3=ActualCore::UnityEngine.Vector3;\n"
        "using ActualMathf=ActualCore::UnityEngine.Mathf;\n"
        + body
    )
    output.mkdir(parents=True, exist_ok=True)
    with (output / OUTPUT).open("w", encoding="utf-8", newline="\n") as file:
        file.write(transformed)
    return {
        "source_pins": {str(source_path.resolve()): hashlib.sha256(raw).hexdigest()},
        "transform": "Mechanical UnityEngine.Vector3/Mathf aliases only; policy body preserved",
        "output_sha256": hashlib.sha256(transformed.encode("utf-8")).hexdigest(),
    }

