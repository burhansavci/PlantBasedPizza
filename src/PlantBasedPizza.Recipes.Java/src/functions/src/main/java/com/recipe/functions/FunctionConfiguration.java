package com.recipe.functions;
import com.amazonaws.services.lambda.runtime.events.SQSEvent;
import com.amazonaws.services.lambda.runtime.events.SQSBatchResponse;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.recipe.functions.events.OrderConfirmedEvent;
import com.recipe.functions.services.IEventHandlerService;
import datadog.trace.api.Trace;
import io.opentracing.Span;
import io.opentracing.util.GlobalTracer;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.context.annotation.Bean;

import java.util.*;
import java.util.function.Function;

@SpringBootApplication(scanBasePackages = "com.recipe")
public class FunctionConfiguration{
    IEventHandlerService eventHandlerService;
    
    @Autowired
    public FunctionConfiguration(IEventHandlerService eventHandlerService){
        this.eventHandlerService = eventHandlerService;
    }
    
    ObjectMapper objectMapper = new ObjectMapper();

    Logger log = LogManager.getLogger(FunctionConfiguration.class);

    public static void main(String[] args) {
        SpringApplication.run(FunctionConfiguration.class, args);
    } 
    
    @Bean
    public Function<SQSEvent, SQSBatchResponse> handleOrderConfirmedEvent() {
        return value -> {
            List<SQSBatchResponse.BatchItemFailure> failures = new ArrayList<>();

            List<SQSEvent.SQSMessage> records = value.getRecords();

            for (int i = 0, recordsSize = records.size(); i < recordsSize; i++) {
                SQSEvent.SQSMessage message = records.get(i);

                var processSuccess = processOrderConfirmedMessage(message);

                if (!processSuccess) {
                    failures.add(SQSBatchResponse.BatchItemFailure.builder().withItemIdentifier(message.getMessageId()).build());
                }
            }
            
            return SQSBatchResponse.builder()
                    .withBatchItemFailures(failures)
                    .build();
        };
    }

    @Trace(operationName = "ProcessingOrderConfirmedMessage", resourceName = "FunctionConfiguration.processOrderConfirmedMessage")
    public boolean processOrderConfirmedMessage(SQSEvent.SQSMessage message) {
        final Span span = GlobalTracer.get().activeSpan();
        
        try {
            if (span != null){
             span.setTag("aws.sqs.messageId", message.getMessageId());   
            }
            
            log.info(String.format("Processing message %s", message.getMessageId()));
            
            EventWrapper wrapper = new EventWrapper(this.objectMapper, message);

            OrderConfirmedEvent event = wrapper.AsOrderConfirmedEvent();

            return eventHandlerService.handle(event);
        }
        catch (Exception exception){
            log.error(exception);
            return false;
        }
    }
}