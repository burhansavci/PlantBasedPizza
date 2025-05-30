package com.recipe.core;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;

import io.opentracing.Span;
import io.opentracing.util.GlobalTracer;
import momento.sdk.CacheClient;
import momento.sdk.auth.CredentialProvider;
import momento.sdk.config.Configurations;
import momento.sdk.responses.cache.GetResponse;
import momento.sdk.responses.cache.SetResponse;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.hibernate.Cache;
import org.springframework.stereotype.Service;

import java.time.Duration;
import java.util.List;
import java.util.Optional;

@Service
public class MomentoRecipeCacheImpl implements RecipeCache {
    private final CacheClient cacheClient;
    private final String cacheName = System.getenv("CACHE_NAME");
    private final ObjectMapper objectMapper;
    Logger log = LogManager.getLogger(MomentoRecipeCacheImpl.class);
    public MomentoRecipeCacheImpl(){
        this.objectMapper = new ObjectMapper();
        cacheClient = CacheClient.create(
                CredentialProvider.fromEnvVar("MOMENTO_API_KEY"),
                Configurations.InRegion.v1(),
                Duration.ofSeconds(300),
                Duration.ofSeconds(30)
        );
    }

    @Override
    public void SetRecipes(List<RecipeDTO> recipes) {
        try {
            log.info("Updating cache...");
            this.Set("all-recipes", this.objectMapper.writeValueAsString(recipes));
        }
        catch (Exception e) {
            // Cache failures should not prevent the application from working
            log.warn(e);
        }
    }

    @Override
    public Optional<List<RecipeDTO>> GetRecipes() {
        var cacheResult = this.Get("all-recipes");

        try {
            if (cacheResult.isPresent()){
                log.info("Cache hit...");
                List<RecipeDTO> recipeList = this.objectMapper.readValue(cacheResult.get(), new TypeReference<>() {});

                return Optional.of(recipeList);
            }
            else {
                return Optional.empty();
            }
        }
        catch (Exception e) {
            log.info("Cache miss...");
            // Cache failures should not prevent the application from working
            log.warn(e);
        }

        return Optional.empty();
    }

    @Override
    public void SetRecipe(RecipeDTO recipe) {
        try {
            log.info("Updating cache...");
            this.Set(String.valueOf(recipe.getId()), this.objectMapper.writeValueAsString(recipe));
        }
        catch (Exception e) {
            // Cache failures should not prevent the application from working
            log.warn(e);
        }
    }

    @Override
    public Optional<RecipeDTO> GetRecipe(String recipeId) {
        var cacheResult = this.Get(String.valueOf(recipeId));

        try {
            if (cacheResult.isPresent()){
                log.info("Cache hit...");
                RecipeDTO recipe = this.objectMapper.readValue(cacheResult.get(), RecipeDTO.class);

                return Optional.of(recipe);
            }
        }
        catch (Exception e) {
            // Cache failures should not prevent the application from working
            log.warn(e);
        }

        return Optional.empty();
    }

    private void Set(String key, String value) {
        final Span span = GlobalTracer.get().activeSpan();

        var setResponse = cacheClient.set(cacheName, key, value).join();

        if (setResponse instanceof SetResponse.Success) {
            span.setTag("cache.store", true);
        } else if (setResponse instanceof SetResponse.Error error) {
            span.setTag("cache.error", true);
            span.setTag("cache.errorMessage", error.getMessage());
            span.setTag("cache.errorCode", error.getErrorCode().name());
            span.setTag("cache.errorLocalMessage", error.getLocalizedMessage());

            if (error.getTransportErrorDetails().isPresent()) {
                span.setTag("cache.transportError", error.getTransportErrorDetails().get().toString());
            }
        }
    }

    private Optional<String> Get(String key) {
        final Span span = GlobalTracer.get().activeSpan();
        var getResponse = cacheClient.get(cacheName, key).join();

        if (getResponse instanceof GetResponse.Hit hit) {
            span.setTag("cache.hit", true);
            return Optional.of(hit.valueString());
        } else if (getResponse instanceof GetResponse.Miss) {
            span.setTag("cache.miss", true);
            return Optional.empty();
        } else if (getResponse instanceof GetResponse.Error error) {
            span.setTag("cache.error", true);
            span.setTag("cache.errorMessage", error.getMessage());
            span.setTag("cache.errorCode", error.getErrorCode().name());
            span.setTag("cache.errorLocalMessage", error.getLocalizedMessage());

            if (error.getTransportErrorDetails().isPresent()) {
                span.setTag("cache.transportError", error.getTransportErrorDetails().get().toString());
            }

            return Optional.empty();
        }
        else {
            return Optional.empty();
        }
    }
}
