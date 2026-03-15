#!/bin/sh

# Wait for Ollama to be ready
echo "Waiting for Ollama service to be available..."
while ! curl -s http://ollama:11434/api/tags > /dev/null; do
    echo "Ollama not available yet, retrying in 5 seconds..."
    sleep 5
done

echo "Ollama is available!"

# Check if models are already downloaded
echo "Checking available models..."
AVAILABLE_MODELS=$(curl -s http://ollama:11434/api/tags | grep -o '"name":"[^"]*"' | cut -d'"' -f4)

# Required models
REQUIRED_MODELS="qwen2.5:7b
nomic-embed-text
phi3:mini"

# Pull missing models
for model in $REQUIRED_MODELS; do
    if ! echo "$AVAILABLE_MODELS" | grep -q "$model"; then
        echo "Downloading model: $model"
        curl -X POST http://ollama:11434/api/pull \
             -H "Content-Type: application/json" \
             -d "{\"name\":\"$model\"}"
        echo "Model $model downloaded successfully"
    else
        echo "Model $model is already available"
    fi
done

echo "All required models are available!"