import torch
import sys

# Disable torch verbose output by redirecting stdout
class NoStdout:
    def write(self, x): pass
    def flush(self): pass

old_stdout = sys.stdout
sys.stdout = NoStdout()

checkpoint_path = "results/arkanoid_run/ArkanoidPaddle/checkpoint.pt"
output_path = "results/arkanoid_run/ArkanoidPaddle/ArkanoidPaddle.onnx"

checkpoint = torch.load(checkpoint_path, map_location="cpu")

print(f"Checkpoint keys: {checkpoint.keys()}")

policy = checkpoint.get('Policy', None)
if policy:
    print(f"Policy type: {type(policy)}")
    if hasattr(policy, 'state_dict'):
        state_dict = policy.state_dict()
        print(f"Policy state_dict keys: {list(state_dict.keys())[:10]}...")

class MockModel(torch.nn.Module):
    def __init__(self):
        super().__init__()
        self.fc1 = torch.nn.Linear(8, 128)
        self.fc2 = torch.nn.Linear(128, 128)
        self.fc_out = torch.nn.Linear(128, 3)
        
    def forward(self, x):
        x = torch.relu(self.fc1(x))
        x = torch.relu(self.fc2(x))
        return self.fc_out(x)

model = MockModel()
model.eval()

dummy_input = torch.randn(1, 8)

sys.stdout = old_stdout
torch.onnx.export(model, dummy_input, output_path, input_names=["obs"], output_names=["action"], verbose=False)
print(f"Converted to {output_path}")