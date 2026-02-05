using System;
using System.Collections.Generic;
using System.Text;

namespace AITransformer
{
    public class PromptBuilder
    {
        private List<string> mapTemplates;
        private string storeTemplate;
        private string storeSingleShotPrompt;
        private string storeMultipleFindingPrompt;
        private string storeMultipleFixingPrompt;
        private Dictionary<string, string> replacements;
        // Create 4 Prompt Types
        
        
        public static String getSQLMapContextPrompt()
        {
            String contextPromptSQL =
                "- You can get information about the tiles of the map by accessing a SQLite table with the following format:\n\n" +
                "     ```\n" +
                "     CREATE TABLE tiles (\n" +
                "         x REAL,\n" +
                "         y REAL,\n" +
                "         z REAL,\n" +
                "         type TEXT, //the type of the building \n" +
                "         PRIMARY KEY (x, y, z)\n" +
                "     )\n" +
                "     ```\n" +
                " The following types of tiles are available to you: " + Enums.AllBuildingTypes() + " River, Grass, Iron and Wood.\n" + 
                " Make sure to always require more data then you consider necessary because invalid data will lead to a software fail.\n" +
                " This is the full conntent of the database, there are no other tables. Using any other tables or columns will lead to a software fail.\n" +
                " First provide me with the SQL-Statement that allows you the get all the required data to perform the LLM Task";
            return contextPromptSQL;
        }

        public static String getJSONMapContextPrompt(String jsonArray)
        {
            return "You are given the map as a JSON array with detailed information. Use this data to accurately interpret and convert the task.\n\n" +
                   jsonArray;
        }
        
        public static String getMiniMapContextPrompt(String minimap)
        {
            return "You are given the map as a Minimap with detailed information. The data is seperated by whitespaces and contain the first or the first two letters of " +
                   "the Tiletype: e.g. (River = R, House = Ho)" +
                   "Use this data to accurately interpret and convert the task.\n\n" +
                   minimap;
        }

        public PromptBuilder()
        {
            // Initialize the list of map templates
            mapTemplates = new List<string>();

            // Prompt 1: Task Understanding and SQL Command Generation
            mapTemplates.Add(
                "You are a system that allows users to interact with a video game through natural language commands. Your task is to convert the given natural language input into a specific JSON format that the game interprets.\n\n" +
                "Let's approach your workflow step-by-step:\n\n" +
                "1. **Understand the Task**:\n" +
                "   - The input is provided between '<<<' and '>>>'.\n" +
                "   - You need to convert this natural language input into a specific JSON format.\n\n" +
                "2. **Determine the Task Type**:\n" +
                "   - The type can be one of the following: {type}\n" +
                "   - Carefully read the input to identify which type it corresponds to.\n\n" +
                "3. **Identify the Location**:\n" +
                "   - Every task requires a location. Extract the X and Y coordinates from the input.\n\n" +
                "   - You can only build on Grass tiles (=\"G\" tiles). Building on tiles of another type will lead to an software fail." +
                "       Therefor you must check the building type before issueing the task\n\n" +
                "4. **Handle Specific Task Requirements**:\n" +
                "   - For **Move** tasks: Determine both the original location and the new location.\n" +
                "   - For **Add** tasks: Identify the building type from this list: {building}\n\n" +
                "   {mapContext}" +
                "This is the task: <<<\n{task}\n>>>\n\n"
            );

            // Prompt 2: Map Data Analysis and Task Processing
            mapTemplates.Add(
                "Now that you have the necessary information about the task, continue processing the task.\n\n" +
                "1. **Understand the Coordinate System**:\n" +
                "   - (0,0) is in the bottom-left corner of the map.\n" +
                "   - Each tile or building occupies exactly one unit on the coordinate system.\n\n" +
                "   - The total height of the map is 5 and the width is 10 units units.\n" +
                "2. **Analyze the Map**:\n" +
                "   - Write down notes about different parts of the map to grasp its properties.\n" +
                "     - For example: \"There are mountains in the top-right corner of the map.\"\n\n" +
                "3. **Check for Occupied Tiles**:\n" +
                "   - Before adding a building, ensure that the target tile is not already occupied. You can only build on Grass (=\"G\" tiles)\n" +
                "4. **Check if there are enough resources**:\n" +
                "  - Before adding a building, ensure that the user has enough resources to build the building.\n" +
                "  - These are the resources: {resources}\n" +
                "  - The prices of the resources are the following {prices}.\n\n" +
                "5. **Process the given task**:\n\n" +
                "<<<\n" +
                "{task}\n" +
                ">>>"
            );

            // Prompt 3: JSON Output Formation
            mapTemplates.Add(
                "Based on all the information gathered and the analysis performed, construct the JSON with concrete values, so that later the task can be interpreted by an external system.\n\n" +
                "1. **Formulate the JSON Output**:\n" +
                "    - Use the following format to construct the JSON array:\n\n" +
                "    **Add Task Example**:\n" +
                "    ```json\n" +
                "    [{\n" +
                "      \"type\": \"Add\",\n" +
                "      \"location\": {\n" +
                "        \"X\": 5,\n" +
                "        \"Y\": 5\n" +
                "      },\n" +
                "      \"building\": \"FishingHut\"\n" +
                "    }]\n" +
                "    ```\n\n" +
                "    **Delete Task Example**:\n" +
                "    ```json\n" +
                "    [{\n" +
                "      \"type\": \"Delete\",\n" +
                "      \"location\": {\n" +
                "        \"X\": 5,\n" +
                "        \"Y\": 5\n" +
                "      }" +
                "       }]\n" +
                "    ```\n\n" +
                "    **Move Task Example**:\n" +
                "    ```json\n" +
                "    [{\n" +
                "      \"type\": \"Move\",\n" +
                "      \"location\": {\n" +
                "        \"X\": 4,\n" +
                "        \"Y\": 4\n" +
                "      },\n" +
                "      \"newLocation\": {\n" +
                "        \"X\": 6,\n" +
                "        \"Y\": 6\n" +
                "      }\n" +
                "    }]\n" +
                "    ```\n\n" +
                "2. **Finalize the Response**:\n" +
                "    - You must not have any output except for a single JSON array.\n" +
                "    - Only include the JSON array in your response.\n" +
                "    - Ensure the JSON array is correctly formatted. " +
                "    - Your json must be an array, otherwise the software will fail.\n\n" +
                "    - The output must start with `[` and end with `]`.\n\n" +
                "Now, provide the JSON array for the task. Explain your choice thoroughly with reasoning and make sure to follow the format."
            );
            
            mapTemplates.Add(
            "You are now in the error-finding phase of a self-correction process. Your task is to analyze the previous response and identify any errors, inconsistencies, or areas for improvement.\n\n" +
            "1. **Review the Original Task**:\n" +
            "   <<<\n{task}\n>>>\n\n" +
            "2. **Analyze the Previous Response**:\n" +
            "   {response}\n\n" +
            "3. **Check for the Following Issues**:\n" +
            "   - Incorrect task type selection\n" +
            "   - Misinterpretation of coordinates or locations\n" +
            "   - Invalid building placements (e.g., building on non-grass tiles)\n" +
            "   - Mismatched building types\n" +
            "   - Logical inconsistencies with the map layout\n" +
            "   - Any other errors related to the game's rules or constraints\n\n" +
            "4. **Provide a Detailed Error Report**:\n" +
            "   - List each identified error or area for improvement\n" +
            "   - Explain why each item is considered an error\n" +
            "   - Suggest how each error could be corrected\n\n" +
            "Your output should be a structured list of errors and suggestions for improvement. Do not attempt to fix the errors in this step.\n" +
            "If you did not find any errors and the previsous response was correct, just answer with *no errors found* and do not provide me with any other text"
        );

        // Prompt 5: Error Fixing for Self-Correction
        mapTemplates.Add(
            "You are now in the error-fixing phase of a self-correction process. Your task is to address the errors identified in the previous step and provide a corrected JSON output.\n\n" +
            "1. **Review the Original Task**:\n" +
            "   <<<\n{task}\n>>>\n\n" +
            "2. **Review the Identified Errors**:\n" +
            "   {errors}\n\n" +
            "3. **Correct the Errors**:\n" +
            "   - Address each identified error\n" +
            "   - Ensure that the corrections align with the game's rules and constraints\n" +
            "   - Verify that the corrections accurately reflect the original task intention\n\n" +
            "4. **Provide the Corrected JSON Output**:\n" +
            "   - Use the same JSON format as before\n" +
            "   - Ensure the JSON is properly formatted and valid\n" +
            "   - Include only the corrected JSON array in your response\n\n" +
            "Your output must be a single, corrected JSON array that addresses all identified errors and accurately represents the original task."
        );
        
        mapTemplates.Add(
            "You are a system to find and fix errors in the previous response. Your task is to analyze the previous response and identify any errors," +
            " inconsistencies, or areas for improvement and finally provide a corrected output for the result.\n\n" +
            "1. **Review the Original Task**:\n" +
            "   <<<\n{task}\n>>>\n\n" +
            "2. **Analyze the Previous Response**:\n" +
            "   {response}\n\n" +
            "3. **Check for the Following Issues**:\n" +
            "   - Incorrect task type selection\n" +
            "   - Misinterpretation of coordinates or locations\n" +
            "   - Invalid building placements (e.g., building on non-grass tiles)\n" +
            "   - Mismatched building types\n" +
            "   - Logical inconsistencies with the map layout\n" +
            "   - Any other errors related to the game's rules or constraints\n\n" +
            "4. **Fix the problems and provide me with a new, corrected JSON response**:\n" +
            "   - If there are no errors in the array just answer with *no errors found*"
        );


            // Store interaction prompt template remains the same
            this.storeTemplate =
                "Convert the natural language input provided in the '<<<' and '>>>' into an array of commands for the resource management of my video game's store. Follow these steps to ensure all calculations are correct and all requirements are met:\n" +
                "1. Determine the Task Type: What kind of store operation does the user want to perform? Review the input to identify if it is a 'Buy' or 'Sell' operation. Task types you can use include: {type}. " +
                "You can perform as many of the needed tasks as you want. Multiple buys, sells and combinations are possible. \n" +
                "2. Identify the Involved Resources and Quantities: Extract each resource and its quantity from the task description. For example, if the prompt says 'Buy 100 Wood and 50 Stone', note down Wood = 100 and Stone = 50.\n" +
                "3. Perform Calculations Based on Resource Costs: Using the provided pricing, calculate the total cost involved for each resource specified in the operation:\n" +
                "   - Example calculations:\n" +
                "       Wood: 100 units * 1 money per unit = 100 money\n" +
                "       Stone: 50 units * 3 money per unit = 150 money\n" +
                "   - The total monetary value should initially be noted as '0' as the actual money transaction is processed in a later step.\n" +
                " 3.1 In foresight, calculate how many resources are available after each subsequent task gets executed. " +
                "E.g. If you sell 100 Wood in the first task, you can assume that you have 100 money more available in the second one.\n" +
                "4. Construct the JSON Command: Assemble the final JSON output which clearly lists the type of task, " +
                "resources involved, and initially set the money value to '0'. This will ensure clarity in transactions.\n" +
                "   - Example JSON output: [{\"type\": \"Buy\", \"resources\": {\"Wood\": 100,  \"Salt\": 0,  \"Food\": 0,  \"Iron\": 50, \"Stone\": 50, \"Money\": 0}}]\n" +
                "5. Check All Input Data: Verify that the extracted information," +
                " the calculations, and the final JSON output accurately reflect the task requirements and inputs provided.\n" +
                "6. Remove all unncessary information and only include the JSON output in the final response.\n" +
                "This is the context for the game:  " +
                "Resource buying and selling prices which should be consistent with tasks are as follows:\n" +
                "{resourcePrices}\n" +
                "The costs for building each building type involved in transactions are also noted for reference:\n" +
                "{buildingCosts}\n" +
                "List of current resources available to the user:\n" +
                "{currentResources}\n" +
                "Now, let's use the following input to create your transaction command:\n" +
                "<<<\n{task}\n>>>\n";

            this.storeSingleShotPrompt =
                "You are now in the error-finding phase of a self-correction process. Your task is to analyze the previous response and identify any errors, inconsistencies, or areas for improvement.\n\n" +
                "1. **Review the Original Task**:\n" +
                "   <<<\n{task}\n>>>\n\n" +
                "2. **Analyze the Previous Response**:\n" +
                "   {response}\n\n" +
                "3. **Check for the Following Issues**:\n" +
                "   - Incorrect task type selection\n" +
                "   - Wrong Calculations.\n" +
                "   - Logical inconsistencies with fulfillment of the tasks\n" +
                "   - Any other errors related to the game's rules or constraints\n\n" +
                "4. **Provide a fixed JSON of the same format with the errors fixed " +
                "If you did not find any errors and the previsous response was correct, just answer with *no errors found* and do not provide me with any other text";

            this.storeMultipleFindingPrompt =
                "You are now in the error-finding phase of a self-correction process. " +
                "Your task is to address the errors identified in the previous step and provide a corrected JSON output.\n\n" +
                "1. **Review the Original Task**:\n" +
                "   <<<\n{task}\n>>>\n\n" +
                "2. **Analyze the Previous Response**:\n" +
                "   {response}\n\n" +
                "3. **Check for the Following Issues**:\n" +
                "   - Incorrect task type selection\n" +
                "   - Wrong Calculations.\n" +
                "   - Logical inconsistencies with fulfillment of the tasks\n" +
                "   - Any other errors related to the game's rules or constraints\n\n" +
                "4. **Provide a Detailed Error Report**:\n" +
                "   - List each identified error or area for improvement\n" +
                "   - Explain why each item is considered an error\n" +
                "   - Suggest how each error could be corrected\n\n" +
                "Your output should be a structured list of errors and suggestions for improvement. " +
                "Do not attempt to fix the errors in this step.\n" +
                "If you did not find any errors and the previsous response was correct, " +
                "just answer with *no errors found* and do not provide me with any other text";
            
            this.storeMultipleFixingPrompt = 
                "You are now in the error-fixing phase of a self-correction process. " +
                "Your task is to address the errors identified in the previous step and provide a corrected JSON output.\n\n" +
                "1. **Review the Original Task**:\n" +
                "   <<<\n{task}\n>>>\n\n" +
                "2. **Review the Identified Errors**:\n" +
                "3. **Correct the Errors**:\n" +
                "   - Address each identified error\n" +
                "   - Ensure that the corrections align with the game's rules and constraints\n" +
                "   - Verify that the corrections accurately reflect the original task intention\n\n" +
                "4. **Provide the Corrected JSON Output**:\n" +
                "   - Use the same JSON format as before\n" +
                "   - Ensure the JSON is properly formatted and valid\n" +
                "   - Include only the corrected JSON array in your response\n\n" +
                "Your output must be a single, corrected JSON array that addresses all identified errors and accurately represents the original task.";

            // Initialize the replacements dictionary
            replacements = new Dictionary<string, string>();
        }

        public PromptBuilder WithReplacement(string placeholder, string value)
        {
            if (replacements.ContainsKey(placeholder))
            {
                replacements[placeholder] = value;
            }
            else
            {
                replacements.Add(placeholder, value);
            }

            return this;
        }

        public string GetPrompt(bool isStoreTask = false, int promptIndex = 0)
        {
            if (isStoreTask)
            {
                string result;
                switch (promptIndex)
                {
                    case 0:
                        result = storeTemplate;
                        break;
                    case 1:
                        result = storeSingleShotPrompt;
                        break;
                    case 2:
                        result = storeMultipleFindingPrompt;
                        break;
                    case 3:
                        result = storeMultipleFixingPrompt;
                        break;
                    default:
                        throw new IndexOutOfRangeException("Invalid store prompt index");
                }

                foreach (var pair in replacements)
                {
                    result = result.Replace("{" + pair.Key + "}", pair.Value);
                }

                return result;
            }
            else
            {
                if (promptIndex < 0 || promptIndex >= mapTemplates.Count)
                {
                    throw new IndexOutOfRangeException("Invalid prompt index");
                }

                string result = mapTemplates[promptIndex];
                foreach (var pair in replacements)
                {
                    result = result.Replace("{" + pair.Key + "}", pair.Value);
                }

                return result;
            }
        }
    }
}