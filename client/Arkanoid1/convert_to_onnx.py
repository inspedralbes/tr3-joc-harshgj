import argparse
from pathlib import Path

import torch
from mlagents.trainers.policy.torch_policy import TorchPolicy
from mlagents.trainers.ppo.optimizer_torch import PPOSettings
from mlagents.trainers.settings import (
    NetworkSettings,
    RewardSignalSettings,
    RewardSignalType,
    ScheduleType,
    TrainerSettings,
)
from mlagents.trainers.torch_entities.model_serialization import (
    ModelSerializer,
    exporting_to_onnx,
)
from mlagents.trainers.torch_entities.networks import SimpleActor
from mlagents_envs.base_env import (
    ActionSpec,
    BehaviorSpec,
    DimensionProperty,
    ObservationSpec,
    ObservationType,
)


ROOT = Path(__file__).resolve().parent
DEFAULT_CHECKPOINT_PATH = ROOT / "results" / "arkanoid_run" / "ArkanoidPaddle" / "checkpoint.pt"
DEFAULT_RESULTS_OUTPUT_BASE = ROOT / "results" / "arkanoid_run" / "ArkanoidPaddle" / "ArkanoidPaddle"
UNITY_MODEL_DIR = ROOT / "Assets" / "Resources" / "Models"


def build_behavior_spec() -> BehaviorSpec:
    observation_spec = ObservationSpec(
        shape=(5,),
        dimension_property=(DimensionProperty.UNSPECIFIED,),
        observation_type=ObservationType.DEFAULT,
        name="obs_0",
    )
    action_spec = ActionSpec.create_discrete((3,))
    return BehaviorSpec([observation_spec], action_spec)


def build_trainer_settings() -> TrainerSettings:
    return TrainerSettings(
        trainer_type="ppo",
        hyperparameters=PPOSettings(
            batch_size=128,
            buffer_size=2048,
            learning_rate=0.0003,
            beta=0.0005,
            epsilon=0.2,
            lambd=0.95,
            num_epoch=3,
            shared_critic=False,
            learning_rate_schedule=ScheduleType.LINEAR,
            beta_schedule=ScheduleType.LINEAR,
            epsilon_schedule=ScheduleType.LINEAR,
        ),
        checkpoint_interval=500000,
        network_settings=NetworkSettings(
            normalize=False,
            hidden_units=128,
            num_layers=2,
            deterministic=False,
        ),
        reward_signals={
            RewardSignalType.EXTRINSIC: RewardSignalSettings(
                gamma=0.99,
                strength=1.0,
                network_settings=NetworkSettings(
                    normalize=False,
                    hidden_units=128,
                    num_layers=2,
                    deterministic=False,
                ),
            )
        },
        keep_checkpoints=5,
        max_steps=1000000,
        time_horizon=128,
        summary_freq=5000,
        threaded=False,
    )


def build_policy() -> TorchPolicy:
    behavior_spec = build_behavior_spec()
    settings = build_trainer_settings()
    return TorchPolicy(
        seed=0,
        behavior_spec=behavior_spec,
        network_settings=settings.network_settings,
        actor_cls=SimpleActor,
        actor_kwargs={
            "conditional_sigma": False,
            "tanh_squash": False,
        },
    )


def load_checkpoint(policy: TorchPolicy, checkpoint_path: Path) -> None:
    checkpoint = torch.load(checkpoint_path, map_location="cpu")
    policy_modules = policy.get_modules()

    missing_keys, unexpected_keys = policy_modules["Policy"].load_state_dict(
        checkpoint["Policy"],
        strict=False,
    )
    if missing_keys:
        raise RuntimeError(f"Missing policy keys in checkpoint: {missing_keys}")
    if unexpected_keys:
        raise RuntimeError(f"Unexpected policy keys in checkpoint: {unexpected_keys}")

    policy_modules["global_step"].load_state_dict(checkpoint["global_step"])


def export_model(serializer: ModelSerializer, output_base: Path) -> None:
    output_base.parent.mkdir(parents=True, exist_ok=True)
    with exporting_to_onnx():
        torch.onnx.export(
            serializer.policy.actor,
            serializer.dummy_input,
            str(output_base.with_suffix(".onnx")),
            opset_version=9,
            input_names=serializer.input_names,
            output_names=serializer.output_names,
            dynamic_axes=serializer.dynamic_axes,
            dynamo=False,
            external_data=False,
        )

def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--checkpoint", type=Path, default=DEFAULT_CHECKPOINT_PATH)
    parser.add_argument("--results-output", type=Path, default=DEFAULT_RESULTS_OUTPUT_BASE)
    parser.add_argument("--unity-name", default="finalpaddle")
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    policy = build_policy()
    load_checkpoint(policy, args.checkpoint)
    serializer = ModelSerializer(policy)

    export_model(serializer, args.results_output)
    print(f"Converted to {args.results_output}.onnx")

    unity_output_base = UNITY_MODEL_DIR / args.unity_name
    export_model(serializer, unity_output_base)
    print(f"Converted to {unity_output_base}.onnx")


if __name__ == "__main__":
    main()
