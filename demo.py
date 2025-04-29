import requests

# API configuration
TEXT_API_URL = "https://api.nekoapi.com/v1/chat/completions"
IMAGE_API_URL = "https://api.nekoapi.com/v1/images/generations"
API_KEY = ""  # Add your API key here

# System prompt for prompt engineering
SYSTEM_PROMPT = """You are a prompt engineering specialist for DALL-E 3. When users describe an image concept, 
always generate a visualization prompt that strictly follows this template:

'A simple line drawing of [SUBJECT DESCRIPTION]. [SUBJECT] has [DEFINING CHARACTERISTIC], with [OPTIONAL DETAIL]. 
[ADD MORE NECESSARY DETAILS IF NEEDED]. [Add composition and background information if necessary].
The drawing is minimalistic, using only black lines on a white background.'

Key requirements:
1. Maintain this exact sentence structure.
2. Stick to the user's description and avoid adding unnecessary details.

Example: "A simple line drawing of a boy eating a hot dog. The boy has a happy expression, with his mouth open as he takes a bite. 
The drawing is minimalistic, using only black lines on a white background."

Respond ONLY with the prompt. No markdown or explanations."""

def generate_enhanced_prompt(user_input):
    """Use GPT-4o-mini to enhance the user's prompt"""
    response = requests.post(
        TEXT_API_URL,
        headers={
            "Content-Type": "application/json",
            "Authorization": f"Bearer {API_KEY}"
        },
        json={
            "model": "gpt-4o-mini",
            "messages": [
                {"role": "system", "content": SYSTEM_PROMPT},
                {"role": "user", "content": user_input}
            ],
            "temperature": 0.7,
            "max_tokens": 150
        }
    )
    
    if response.status_code == 200:
        return response.json()['choices'][0]['message']['content'].strip()
    raise Exception(f"Prompt enhancement failed: {response.text}")

def generate_image(prompt):
    """Generate image using DALL-E 3 with enhanced prompt"""
    response = requests.post(
        IMAGE_API_URL,
        headers={
            "Content-Type": "application/json",
            "Authorization": f"Bearer {API_KEY}"
        },
        json={
            "model": "dall-e-3",
            "prompt": prompt,
            "n": 1,
            "size": "1024x1024"
        }
    )
    
    if response.status_code == 200:
        return response.json()['data'][0]['url']
    raise Exception(f"Image generation failed: {response.text}")

if __name__ == "__main__":
    try:
        user_prompt = input("Enter your image description: ")
        print("\nOptimizing prompt...")
        enhanced_prompt = generate_enhanced_prompt(user_prompt)
        
        print("\nGenerated DALL-E 3 Prompt:", enhanced_prompt)
        print("\nGenerating image...")
        
        image_url = generate_image(enhanced_prompt)
        print(f"\nImage created successfully!\nURL: {image_url}")
    
    except Exception as e:
        print(f"\nError: {str(e)}")