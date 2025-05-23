import axios from "axios";

const recipeApi = axios.create({
  baseURL: "https://api.dev.plantbasedpizza.net/recipes",
});

const recipeService = {
  listRecipes: async function () {
    const response = await recipeApi.get('/');

    return response.data;
  },
};

export default recipeService;
