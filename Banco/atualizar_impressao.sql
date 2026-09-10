USE dbRestaurante;

-- Nova coluna: quantidade que já foi enviada para a cozinha.
ALTER TABLE ItensPedido
    ADD COLUMN QuantidadeImpressa INT NOT NULL DEFAULT 0 AFTER Quantidade;

-- Converte os registros antigos:
-- se Impresso=1, considera que a quantidade atual já foi impressa.
UPDATE ItensPedido
SET QuantidadeImpressa = CASE
    WHEN Impresso = 1 THEN Quantidade
    ELSE 0
END;

-- Mantém o campo antigo coerente com a nova lógica.
UPDATE ItensPedido
SET Impresso = CASE
    WHEN QuantidadeImpressa >= Quantidade THEN 1
    ELSE 0
END;
