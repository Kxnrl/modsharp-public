import { Instance, Console } from "cs_script/point_script";

const scoreLimit = 1000;
const checkpointState = { total: 0 };
let awardCount = 0;

function acknowledgeAward(checkpoint, total) {
    Console.Log(`[CheckpointBridge] C# called JS: ${checkpoint}, total=${total}`);
    return true;
}

function submitAward(points) {
    try {
        const receipt = Instance.ModSharpDocs_CheckpointAwardV1(
            "training_gate", points, acknowledgeAward);
        awardCount++;
        Console.Log(
            `[CheckpointBridge] award #${awardCount}: ${receipt.awards.join("+")} ` +
            `= ${receipt.awarded}; total=${receipt.total}/${scoreLimit}; ` +
            `JS state=${checkpointState.total}; acknowledged=${receipt.acknowledged}`);
    } catch (error) {
        Console.Log(`[CheckpointBridge] award rejected: ${String(error)}`);
    }
}

// Hammer I/O: target this point_script, input RunScriptInput, parameter award.
Instance.OnScriptInput("award", () => submitAward([10, 15]));
Instance.OnScriptInput("invalid_award", () => submitAward([-1]));
Console.Log("[CheckpointBridge] Ready: RunScriptInput award or invalid_award.");
