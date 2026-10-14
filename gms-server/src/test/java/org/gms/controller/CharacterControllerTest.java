package org.gms.controller;

import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.gms.model.dto.SubmitBody;
import org.gms.service.CharacterService;
import org.junit.jupiter.api.Test;

import java.util.Map;

import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.mockito.Mockito.doThrow;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.verify;

class CharacterControllerTest {
    @Test
    void changeNameAcceptsWrappedRequestBody() throws Exception {
        CharacterService characterService = mock(CharacterService.class);
        CharacterController controller = new CharacterController(characterService);
        SubmitBody<Map<String, String>> request = new ObjectMapper().readValue(
                "{\"requestId\":\"request-1\",\"data\":{\"newName\":\"NewName\"}}",
                new TypeReference<>() {
                });
        doThrow(new IllegalStateException("stop after service invocation"))
                .when(characterService).changeCharacterName(123, "NewName");

        assertThrows(IllegalStateException.class, () -> controller.changeName(123, request));

        verify(characterService).changeCharacterName(123, "NewName");
    }
}
