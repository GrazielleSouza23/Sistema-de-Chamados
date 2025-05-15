-- Sequence para gerar IDs únicos para a tabela Usuario
CREATE SEQUENCE usuario_seq START WITH 1 INCREMENT BY 1 NOCACHE;

-- Sequence para gerar IDs únicos para a tabela Chamado
CREATE SEQUENCE chamado_seq START WITH 1 INCREMENT BY 1 NOCACHE;

-- Sequence para gerar IDs únicos para a tabela Avaliacao
CREATE SEQUENCE avaliacao_seq START WITH 1 INCREMENT BY 1 NOCACHE;

-- Sequence para gerar IDs únicos para a tabela BaseConhecimento
CREATE SEQUENCE baseconhecimento_seq START WITH 1 INCREMENT BY 1 NOCACHE;

-- Sequence para gerar IDs únicos para a tabela Notificacao
CREATE SEQUENCE notificacao_seq START WITH 1 INCREMENT BY 1 NOCACHE;

-- Sequence para gerar IDs únicos para a tabela HistoricoChamado
CREATE SEQUENCE historicochamado_seq START WITH 1 INCREMENT BY 1;

-- Sequence para gerar IDs únicos para a tabela RegistroTempo
CREATE SEQUENCE registrotempo_seq START WITH 1 INCREMENT BY 1 NOCACHE;

-- Sequence para gerar IDs únicos para a tabela SLA
CREATE SEQUENCE sla_seq START WITH 1 INCREMENT BY 1 NOCACHE;

-- Sequence para gerar IDs únicos para a tabela Escalonamento
CREATE SEQUENCE escalonamento_seq START WITH 1 INCREMENT BY 1 NOCACHE;

-- Usuário
CREATE TABLE Usuario (
    ID NUMBER PRIMARY KEY, -- Será populado via sequence
    Nome VARCHAR2(100),
    Email VARCHAR2(100) UNIQUE,
    DataNascimento DATE,
    TipoDeUsuario VARCHAR2(20) CHECK (TipoDeUsuario IN ('COMUM', 'TECNICO', 'GERENTE', 'RH')),
    Departamento VARCHAR2(100),
    Telefone VARCHAR2(20),
    DataCadastro DATE,
    Status VARCHAR2(20) CHECK (Status IN ('ATIVO', 'INATIVO')),
    Senha VARCHAR2(100)
);

-- Departamento
CREATE TABLE Departamento (
    ID NUMBER PRIMARY KEY, -- Pode ser populado manualmente ou via sequence
    Nome VARCHAR2(100),
    Descricao VARCHAR2(200)
);

-- Chamado
CREATE TABLE Chamado (
    ID NUMBER PRIMARY KEY, -- Será populado via sequence
    Descricao VARCHAR2(500),
    DataAbertura DATE,
    DataFechamento DATE,
    Status VARCHAR2(20) CHECK (Status IN ('ABERTO', 'EM_ANDAMENTO', 'RESOLVIDO', 'FECHADO')),
    NivelUrgencia VARCHAR2(20) CHECK (NivelUrgencia IN ('BAIXO', 'MEDIO', 'ALTO', 'CRITICO')),
    Categoria VARCHAR2(100),
    TempoTotalAtendimento NUMBER, -- Em minutos, pode ser atualizado ao registrar tempo
    ID_Solicitante NUMBER,
    ID_TecnicoResponsavel NUMBER,
    SolucaoAplicada VARCHAR2(1000),
    FOREIGN KEY (ID_Solicitante) REFERENCES Usuario(ID),
    FOREIGN KEY (ID_TecnicoResponsavel) REFERENCES Usuario(ID)
);

-- Avaliação
CREATE TABLE Avaliacao (
    ID NUMBER PRIMARY KEY, -- Será populado via sequence
    ID_Chamado NUMBER UNIQUE, -- Garante 1 avaliação por chamado
    ID_Usuario NUMBER, -- Usuário que fez a avaliação (normalmente o solicitante)
    Nota NUMBER CHECK (Nota BETWEEN 0 AND 10),
    Comentario VARCHAR2(500),
    DataAvaliacao DATE,
    FOREIGN KEY (ID_Chamado) REFERENCES Chamado(ID),
    FOREIGN KEY (ID_Usuario) REFERENCES Usuario(ID)
);

-- Base de Conhecimento
CREATE TABLE BaseConhecimento (
    ID NUMBER PRIMARY KEY, -- Será populado via sequence
    Titulo VARCHAR2(200),
    Descricao VARCHAR2(500),
    Solucao VARCHAR2(1000),
    Categoria VARCHAR2(100),
    DataCriacao DATE,
    DataAtualizacao DATE,
    ID_Autor NUMBER, -- Usuário que criou/atualizou o item
    FOREIGN KEY (ID_Autor) REFERENCES Usuario(ID)
);

-- Habilidade
CREATE TABLE Habilidade (
    ID NUMBER PRIMARY KEY, -- Pode ser populado manualmente ou via sequence
    Nome VARCHAR2(100) UNIQUE,
    Descricao VARCHAR2(300)
);

-- Habilidade_Tecnico (relacionamento N:N)
CREATE TABLE Habilidade_Tecnico (
    ID_Tecnico NUMBER,
    ID_Habilidade NUMBER,
    PRIMARY KEY (ID_Tecnico, ID_Habilidade),
    FOREIGN KEY (ID_Tecnico) REFERENCES Usuario(ID),
    FOREIGN KEY (ID_Habilidade) REFERENCES Habilidade(ID)
);

-- Notificação
CREATE TABLE Notificacao (
    ID NUMBER PRIMARY KEY, -- Será populado via sequence
    Mensagem VARCHAR2(500),
    DataEnvio DATE,
    Lida CHAR(1) CHECK (Lida IN ('S', 'N')),
    ID_UsuarioDestinatario NUMBER,
    ID_ChamadoRelacionado NUMBER,
    Tipo VARCHAR2(30), -- Tipos mais específicos (ATUALIZACAO, RESOLUCAO, ALERTA, ESCALONAMENTO, POTENCIALMENTE_ATRASADO, etc.)
    FOREIGN KEY (ID_UsuarioDestinatario) REFERENCES Usuario(ID),
    FOREIGN KEY (ID_ChamadoRelacionado) REFERENCES Chamado(ID)
);

-- Registro de Tempo
CREATE TABLE RegistroTempo (
    ID NUMBER PRIMARY KEY, -- Será populado via sequence
    ID_Chamado NUMBER,
    ID_Tecnico NUMBER,
    DataRegistro DATE,
    TempoDedicado NUMBER, -- Em minutos
    DescricaoAtividade VARCHAR2(500),
    FOREIGN KEY (ID_Chamado) REFERENCES Chamado(ID),
    FOREIGN KEY (ID_Tecnico) REFERENCES Usuario(ID)
);

-- Folha de Pagamento (Estrutura simplificada)
CREATE TABLE FolhaPagamento (
    ID NUMBER PRIMARY KEY, -- Pode ser populado via sequence
    ID_Usuario NUMBER UNIQUE, -- Para evitar múltiplos registros por usuário na simulação
    Mes NUMBER CHECK (Mes BETWEEN 1 AND 12),
    Ano NUMBER,
    HorasNormais NUMBER,
    HorasExtras NUMBER,
    Adicionais NUMBER,
    Total NUMBER,
    FOREIGN KEY (ID_Usuario) REFERENCES Usuario(ID)
);

-- Relatório (Estrutura simplificada para armazenar metadados ou conteúdo)
CREATE TABLE Relatorio (
    ID NUMBER PRIMARY KEY, -- Pode ser populado via sequence
    Tipo VARCHAR2(50),
    DataGeracao DATE,
    PeriodoInicial DATE,
    PeriodoFinal DATE,
    Conteudo CLOB, -- Armazenar o relatório como texto ou JSON, etc.
    ID_UsuarioGerador NUMBER,
    FOREIGN KEY (ID_UsuarioGerador) REFERENCES Usuario(ID)
);

-- SLA (Service Level Agreement)
CREATE TABLE SLA (
    ID NUMBER PRIMARY KEY, -- Será populado via sequence
    CategoriaChamado VARCHAR2(100),
    NivelUrgencia VARCHAR2(20) CHECK (NivelUrgencia IN ('BAIXO', 'MEDIO', 'ALTO', 'CRITICO')),
    TempoMaximoResolucao INTERVAL DAY TO SECOND,
    Descricao VARCHAR2(300),
    UNIQUE (CategoriaChamado, NivelUrgencia) -- Um SLA único por combinação de Categoria/Urgência
);

-- Escalonamento
CREATE TABLE Escalonamento (
    ID NUMBER PRIMARY KEY, -- Será populado via sequence
    ID_Chamado NUMBER,
    NivelEscalonamento NUMBER, -- Ex: 1 (para Gerente), 2 (para Diretoria), etc.
    DataEscalonamento DATE,
    ID_UsuarioAnterior NUMBER, -- Quem estava responsável antes (pode ser NULL)
    ID_UsuarioNovoResponsavel NUMBER, -- Quem se tornou responsável (pode ser Gerente, outro Técnico, etc.)
    Motivo VARCHAR2(500),
    FOREIGN KEY (ID_Chamado) REFERENCES Chamado(ID),
    FOREIGN KEY (ID_UsuarioAnterior) REFERENCES Usuario(ID),
    FOREIGN KEY (ID_UsuarioNovoResponsavel) REFERENCES Usuario(ID)
);

-- Histórico de Chamado
CREATE TABLE HistoricoChamado (
    ID NUMBER PRIMARY KEY, -- Populado por sequence
    ID_Chamado NUMBER NOT NULL, -- Referência ao chamado original
    DataAtualizacao DATE DEFAULT SYSDATE, -- Quando foi feita a atualização
    DescricaoAtualizacao VARCHAR2(500), -- Texto com o que foi alterado
    ID_UsuarioAtualizou NUMBER NOT NULL, -- Quem fez a alteração
    FOREIGN KEY (ID_Chamado) REFERENCES Chamado(ID),
    FOREIGN KEY (ID_UsuarioAtualizou) REFERENCES Usuario(ID)
);


---INSERIR DADOS

INSERT INTO Departamento(ID, Nome, Descricao) VALUES(1, 'TI', 'Cuida da infraestrutura tecnológica da empresa, incluindo suporte técnico, segurança da informação e desenvolvimento de sistemas. ' );
INSERT INTO Departamento(ID, Nome, Descricao) VALUES(2, 'RH','Cuida das relações de trabalho, incluindo recrutamento, seleção, treinamento, desenvolvimento e remuneração de funcionários.');
INSERT INTO Departamento(ID, Nome, Descricao) VALUES(3, 'Financeiro','Responsável pela gestão de recursos financeiros, como controle de caixa, investimentos, contas a pagar e receber, e planejamento financeiro.');
INSERT INTO Departamento(ID, Nome, Descricao) VALUES(4, 'Administrativo','Envolve a gestão geral da empresa, incluindo planejamento estratégico, tomada de decisões e supervisão dos outros departamentos.');
INSERT INTO Departamento(ID, Nome, Descricao) VALUES(5, 'Comercial/Vendas', 'Atua na prospecção de clientes, negociação de contratos e aumento das vendas. ');
INSERT INTO Departamento(ID, Nome, Descricao) VALUES(6, 'Marketing','Elabora estratégias para promover a marca, produtos ou serviços da empresa, incluindo campanhas publicitárias e gestão de redes sociais.');
INSERT INTO Departamento(ID, Nome, Descricao) VALUES(7, 'Juridico', 'Procura garantir que a empresa esteja em conformidade com as leis e regulamentações.');
INSERT INTO Departamento(ID, Nome, Descricao) VALUES(8, 'Logistico','Gerencia a cadeia de suprimentos, incluindo transporte, armazenamento e distribuição de produtos. ');
INSERT INTO Departamento(ID, Nome, Descricao) VALUES(9, 'Produção', 'Responsável pela criação de bens ou serviços, incluindo gestão de estoque, planejamento da produção e controle de qualidade. ');

-- REDES
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (1, 'Redes', 'CRITICO', INTERVAL '2:00:00' HOUR TO SECOND, 'Rede corporativa fora do ar, afetando todos os usuários');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (2, 'Redes', 'ALTO', INTERVAL '4:00:00' HOUR TO SECOND, 'Conectividade instável em setores inteiros');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (3, 'Redes', 'MEDIO', INTERVAL '8:00:00' HOUR TO SECOND, 'Problemas de rede em um ou poucos dispositivos');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (4, 'Redes', 'BAIXO', INTERVAL '24:00:00' HOUR TO SECOND, 'Solicitação de alteração de configuração sem urgência');

-- HARDWARE
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (5, 'Hardware', 'CRITICO', INTERVAL '3:00:00' HOUR TO SECOND, 'Servidor crítico inoperante');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (6, 'Hardware', 'ALTO', INTERVAL '6:00:00' HOUR TO SECOND, 'Estação de trabalho inoperante em área essencial');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (7, 'Hardware', 'MEDIO', INTERVAL '12:00:00' HOUR TO SECOND, 'Problemas em periféricos ou equipamentos secundários');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (8, 'Hardware', 'BAIXO', INTERVAL '48:00:00' HOUR TO SECOND, 'Upgrade ou melhoria solicitada sem urgência');

-- SISTEMAS OPERACIONAIS
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (9, 'Sistemas Operacionais', 'CRITICO', INTERVAL '4:00:00' HOUR TO SECOND, 'Falha geral de sistema impedindo login de múltiplos usuários');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (10, 'Sistemas Operacionais', 'ALTO', INTERVAL '8:00:00' HOUR TO SECOND, 'Erro de sistema impedindo uso de aplicações essenciais');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (11, 'Sistemas Operacionais', 'MEDIO', INTERVAL '24:00:00' HOUR TO SECOND, 'Problemas recorrentes de configuração ou performance');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (12, 'Sistemas Operacionais', 'BAIXO', INTERVAL '48:00:00' HOUR TO SECOND, 'Solicitação de suporte ou ajuste sem impacto imediato');

-- SEGURANÇA DE SISTEMAS
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (13, 'Segurança de Sistemas', 'CRITICO', INTERVAL '2:00:00' HOUR TO SECOND, 'Vazamento ou ameaça crítica identificada');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (14, 'Segurança de Sistemas', 'ALTO', INTERVAL '6:00:00' HOUR TO SECOND, 'Atividades suspeitas ou falha em sistemas de proteção');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (15, 'Segurança de Sistemas', 'MEDIO', INTERVAL '24:00:00' HOUR TO SECOND, 'Configuração ou revisão de segurança necessária');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (16, 'Segurança de Sistemas', 'BAIXO', INTERVAL '72:00:00' HOUR TO SECOND, 'Solicitação de auditoria ou relatório de rotina');

-- BANCO DE DADOS
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (17, 'Banco de Dados', 'CRITICO', INTERVAL '2:00:00' HOUR TO SECOND, 'Banco de dados inacessível ou corrompido');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (18, 'Banco de Dados', 'ALTO', INTERVAL '6:00:00' HOUR TO SECOND, 'Problemas de performance em banco crítico');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (19, 'Banco de Dados', 'MEDIO', INTERVAL '24:00:00' HOUR TO SECOND, 'Erros ou lentidão em consultas específicas');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (20, 'Banco de Dados', 'BAIXO', INTERVAL '72:00:00' HOUR TO SECOND, 'Backup, análise ou otimização agendada');

-- VIRTUALIZAÇÃO
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (21, 'Virtualização', 'CRITICO', INTERVAL '2:00:00' HOUR TO SECOND, 'Máquina virtual crítica inoperante');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (22, 'Virtualização', 'ALTO', INTERVAL '6:00:00' HOUR TO SECOND, 'Erro em host ou VM com impacto em produção');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (23, 'Virtualização', 'MEDIO', INTERVAL '24:00:00' HOUR TO SECOND, 'Solicitação de recurso ou ajuste em VMs');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (24, 'Virtualização', 'BAIXO', INTERVAL '72:00:00' HOUR TO SECOND, 'Criação de VMs para testes ou ambiente de homologação');

-- DESENVOLVIMENTO DE SOFTWARE
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (25, 'Desenvolvimento de Software', 'CRITICO', INTERVAL '4:00:00' HOUR TO SECOND, 'Falha em sistema customizado impactando operação');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (26, 'Desenvolvimento de Software', 'ALTO', INTERVAL '8:00:00' HOUR TO SECOND, 'Erro funcional em software entregue');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (27, 'Desenvolvimento de Software', 'MEDIO', INTERVAL '48:00:00' HOUR TO SECOND, 'Ajustes ou melhorias com prioridade média');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (28, 'Desenvolvimento de Software', 'BAIXO', INTERVAL '96:00:00' HOUR TO SECOND, 'Solicitação de nova funcionalidade sem urgência');

-- SUPORTE AO USUÁRIO
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (29, 'Suporte ao Usuário', 'CRITICO', INTERVAL '2:00:00' HOUR TO SECOND, 'Impossibilidade total de trabalho de usuário-chave');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (30, 'Suporte ao Usuário', 'ALTO', INTERVAL '4:00:00' HOUR TO SECOND, 'Problema que impede operação de tarefas rotineiras');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (31, 'Suporte ao Usuário', 'MEDIO', INTERVAL '12:00:00' HOUR TO SECOND, 'Dificuldades moderadas em tarefas específicas');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (32, 'Suporte ao Usuário', 'BAIXO', INTERVAL '24:00:00' HOUR TO SECOND, 'Dúvidas operacionais ou configurações opcionais');

-- TELEFONIA IP
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (33, 'Telefonia IP', 'CRITICO', INTERVAL '2:00:00' HOUR TO SECOND, 'Sistema de telefonia corporativa totalmente indisponível');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (34, 'Telefonia IP', 'ALTO', INTERVAL '6:00:00' HOUR TO SECOND, 'Falhas de chamadas recorrentes ou quedas de conexão');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (35, 'Telefonia IP', 'MEDIO', INTERVAL '24:00:00' HOUR TO SECOND, 'Problemas com ramais ou configurações de usuários');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (36, 'Telefonia IP', 'BAIXO', INTERVAL '72:00:00' HOUR TO SECOND, 'Solicitação de criação de ramal ou mudança de configuração');

-- ADMINISTRAÇÃO DE REDES
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (37, 'Administração de Redes', 'CRITICO', INTERVAL '2:00:00' HOUR TO SECOND, 'Problema de roteamento afetando múltiplas unidades');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (38, 'Administração de Redes', 'ALTO', INTERVAL '6:00:00' HOUR TO SECOND, 'Reconfiguração necessária em switches/firewalls produtivos');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (39, 'Administração de Redes', 'MEDIO', INTERVAL '24:00:00' HOUR TO SECOND, 'Ajustes programados em rede de segmento não crítico');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (40, 'Administração de Redes', 'BAIXO', INTERVAL '72:00:00' HOUR TO SECOND, 'Solicitação de acesso ou revisão de documentação da rede');

--IMPRESSORA
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (41, 'Impressora', 'CRITICO', INTERVAL '2:00:00' HOUR TO SECOND, 'Todas as impressoras do setor financeiro pararam simultaneamente, impedindo faturamento');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (42, 'Impressora', 'ALTO', INTERVAL '6:00:00' HOUR TO SECOND, 'Impressora compartilhada fora do ar em área crítica');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (43, 'Impressora', 'MEDIO', INTERVAL '12:00:00' HOUR TO SECOND, 'Impressora com falhas recorrentes, mas ainda funcional');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (44, 'Impressora', 'BAIXO', INTERVAL '48:00:00' HOUR TO SECOND, 'Solicitação de instalação de nova impressora pessoal sem urgência');

--ACESSO A SISTEMAS
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (45, 'Acesso a Sistemas', 'CRITICO', INTERVAL '2:00:00' HOUR TO SECOND, 'Usuário sem acesso a ERP que afeta diretamente operações críticas');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (46, 'Acesso a Sistemas', 'ALTO', INTERVAL '4:00:00' HOUR TO SECOND, 'Erro de autenticação em ERP impedindo o login de usuários em horário de pico');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (47, 'Acesso a Sistemas', 'MEDIO', INTERVAL '8:00:00' HOUR TO SECOND, 'Problema de autenticação no ERP sem impacto imediato nas operações');
INSERT INTO SLA (ID, CategoriaChamado, NivelUrgencia, TempoMaximoResolucao, Descricao) VALUES (48, 'Acesso a Sistemas', 'BAIXO', INTERVAL '24:00:00' HOUR TO SECOND, 'Usuário com erro de login no ERP que pode ser resolvido no próximo expediente');

INSERT INTO Habilidade (ID, Nome, Descricao) VALUES (1, 'Redes', 'Habilidade em configurar, monitorar e solucionar problemas de redes de computadores, incluindo protocolos como TCP/IP, roteadores e switches.');
INSERT INTO Habilidade (ID, Nome, Descricao) VALUES (2, 'Hardware', 'Conhecimento em diagnosticar e reparar falhas em componentes de hardware, como placas-mãe, memórias, HDs e outros periféricos.');
INSERT INTO Habilidade (ID, Nome, Descricao) VALUES (3, 'Sistemas Operacionais', 'Capacidade de instalar, configurar e solucionar problemas relacionados a sistemas operacionais como Windows, Linux e macOS.');
INSERT INTO Habilidade (ID, Nome, Descricao) VALUES (4, 'Segurança de Sistemas', 'Habilidade em proteger sistemas contra ameaças, configurar firewalls, antivírus e realizar auditorias de segurança.');
INSERT INTO Habilidade (ID, Nome, Descricao) VALUES (5, 'Banco de Dados', 'Conhecimento em administrar e otimizar bancos de dados, como SQL Server, MySQL e Oracle, além de realizar backups e recuperação de dados.');
INSERT INTO Habilidade (ID, Nome, Descricao) VALUES (6, 'Virtualização', 'Habilidade de configurar e gerenciar plataformas de virtualização, como VMware, Hyper-V e Docker.');
INSERT INTO Habilidade (ID, Nome, Descricao) VALUES (7, 'Desenvolvimento de Software', 'Conhecimento em linguagens de programação e frameworks, como Python, Java, JavaScript, para desenvolvimento de sistemas e aplicações.');
INSERT INTO Habilidade (ID, Nome, Descricao) VALUES (8, 'Suporte ao Usuário', 'Habilidade de fornecer suporte técnico remoto e presencial a usuários, solucionando problemas com software, hardware e configuração de dispositivos.');
INSERT INTO Habilidade (ID, Nome, Descricao) VALUES (9, 'Telefonia IP', 'Conhecimento em configurar e solucionar problemas em sistemas de telefonia IP, como VoIP, configuração de PBX e comunicação corporativa.');
INSERT INTO Habilidade (ID, Nome, Descricao) VALUES (10, 'Administração de Redes', 'Habilidade em gerenciar redes corporativas, realizando análise de tráfego, controle de acessos e ajustes em configurações de rede.');
INSERT INTO Habilidade (ID, Nome, Descricao) VALUES (11, 'Impressora', 'Conhecimento em configurar e solucionar problemas em impressoras');
INSERT INTO Habilidade (ID, Nome, Descricao) VALUES (12, 'Acesso a Sistemas', 'Conhecimento e habilidade para resolver erro de autenticação em ERP e usuário sem login');

INSERT INTO BaseConhecimento (ID, Titulo, Descricao, Solucao, Categoria, DataCriacao, DataAtualizacao) VALUES(1, 'Impressora não imprime', 'Impressora aparece como offline para o usuário.', 'Verificar conexão USB/rede e reinstalar driver.', 'Impressora', TO_DATE('01/03/2025', 'DD/MM/YYYY'), TO_DATE('01/03/2025', 'DD/MM/YYYY'));
INSERT INTO BaseConhecimento (ID, Titulo, Descricao, Solucao, Categoria, DataCriacao, DataAtualizacao) VALUES(2, 'Falha de rede no setor A', 'Sem acesso à internet em estações do setor A.', 'Reset no switch, troca de cabo principal.', 'Redes', TO_DATE('05/03/2025', 'DD/MM/YYYY'), TO_DATE('06/03/2025', 'DD/MM/YYYY'));
INSERT INTO BaseConhecimento (ID, Titulo, Descricao, Solucao, Categoria, DataCriacao, DataAtualizacao) VALUES(3, 'Usuário sem acesso ao ERP', 'Erro de autenticação ao entrar no sistema ERP.', 'Recriado usuário e redefiniu permissões.', 'Acesso a Sistemas', TO_DATE('10/03/2025', 'DD/MM/YYYY'), TO_DATE('10/03/2025', 'DD/MM/YYYY'));
INSERT INTO BaseConhecimento (ID, Titulo, Descricao, Solucao, Categoria, DataCriacao, DataAtualizacao) VALUES(4, 'Computador não liga', 'Estação não responde ao botão de ligar.', 'Substituição da fonte de alimentação.', 'Hardware', TO_DATE('15/03/2025', 'DD/MM/YYYY'), TO_DATE('16/03/2025', 'DD/MM/YYYY'));
INSERT INTO BaseConhecimento (ID, Titulo, Descricao, Solucao, Categoria, DataCriacao, DataAtualizacao) VALUES(5, 'E-mail não envia mensagens', 'Mensagens ficam na caixa de saída do Outlook.', 'Corrigida configuração SMTP e porta TLS.', 'Suporte ao Usuário', TO_DATE('20/03/2025', 'DD/MM/YYYY'), TO_DATE('20/03/2025', 'DD/MM/YYYY'));
INSERT INTO BaseConhecimento (ID, Titulo, Descricao, Solucao, Categoria, DataCriacao, DataAtualizacao) VALUES(6, 'Impressora não conecta com a máquina','A impressora não estabelece conexão com a máquina do usuário, impedindo a impressão. Em empresas de porte médio, quando localizada em setores operacionais ou financeiros,
pode impactar diretamente nas atividades essenciais.','Verificar drivers, conexões de rede/USB, configurações da impressora e da estação de trabalho; reinstalar impressora se necessário','Impressora',TO_DATE('09/05/2025', 'DD/MM/YYYY'), TO_DATE('09/05/2025', 'DD/MM/YYYY'));


INSERT INTO Usuario (ID, Nome, Email, DataNascimento, TipoDeUsuario, Departamento, Telefone, DataCadastro, Status, Senha) VALUES
(usuario_seq.NEXTVAL, 'Grazielle Souza', 'graziellesouza@gmail.com', TO_DATE('23/05/2005', 'DD/MM/YYYY'), 'TECNICO', 'TI', '19982789150', TO_DATE('09/05/2025', 'DD/MM/YYYY'), 'ATIVO', '123');
INSERT INTO Usuario (ID, Nome, Email, DataNascimento, TipoDeUsuario, Departamento, Telefone, DataCadastro, Status, Senha) VALUES
(usuario_seq.NEXTVAL, 'Gabrielle Souza', 'gabriellesouza@gmail.com', TO_DATE('23/05/2005', 'DD/MM/YYYY'), 'COMUM', 'Financeiro', '19992559916', TO_DATE('09/05/2025', 'DD/MM/YYYY'), 'ATIVO', '123');
INSERT INTO Usuario (ID, Nome, Email, DataNascimento, TipoDeUsuario, Departamento, Telefone, DataCadastro, Status, Senha) VALUES
(usuario_seq.NEXTVAL, 'Julia Souza', 'juliasouza@gmail.com', TO_DATE('27/03/2000', 'DD/MM/YYYY'), 'GERENTE', 'TI', '19123456789', TO_DATE('09/05/2025', 'DD/MM/YYYY'), 'ATIVO', '123');
INSERT INTO Usuario (ID, Nome, Email, DataNascimento, TipoDeUsuario, Departamento, Telefone, DataCadastro, Status, Senha) VALUES
(usuario_seq.NEXTVAL, 'Marilene Souza', 'marilenesouza@gmail.com', TO_DATE('24/11/1975', 'DD/MM/YYYY'), 'RH', 'RH', '19987019564', TO_DATE('09/05/2025', 'DD/MM/YYYY'), 'ATIVO', '123');
INSERT INTO Usuario (ID, Nome, Email, DataNascimento, TipoDeUsuario, Departamento, Telefone, DataCadastro, Status, Senha) VALUES
(usuario_seq.NEXTVAL, 'Maria Oliveira', 'mariaoliveira@gmail.com', TO_DATE('25/04/1987', 'DD/MM/YYYY'), 'TECNICO', 'TI', '19987854147', TO_DATE('09/05/2025', 'DD/MM/YYYY'), 'ATIVO', '123');
INSERT INTO Usuario (ID, Nome, Email, DataNascimento, TipoDeUsuario, Departamento, Telefone, DataCadastro, Status, Senha) VALUES
(usuario_seq.NEXTVAL, 'João Campos', 'joaocampos@gmail.com', TO_DATE('01/08/1995', 'DD/MM/YYYY'), 'TECNICO', 'TI', '19987001452', TO_DATE('09/05/2025', 'DD/MM/YYYY'), 'ATIVO', '123');
INSERT INTO Usuario (ID, Nome, Email, DataNascimento, TipoDeUsuario, Departamento, Telefone, DataCadastro, Status, Senha) VALUES
(usuario_seq.NEXTVAL, 'Carlos Pinheiro', 'carlos@gmail.com', TO_DATE('24/05/1998', 'DD/MM/YYYY'), 'TECNICO', 'TI', '19745681245', TO_DATE('09/05/2025', 'DD/MM/YYYY'), 'ATIVO', '123');
INSERT INTO Usuario (ID, Nome, Email, DataNascimento, TipoDeUsuario, Departamento, Telefone, DataCadastro, Status, Senha) VALUES
(usuario_seq.NEXTVAL, 'Arthur Martins', 'arthurmartins@gmail.com', TO_DATE('01/02/2004', 'DD/MM/YYYY'), 'TECNICO', 'TI', '19987654321', TO_DATE('09/05/2025', 'DD/MM/YYYY'), 'ATIVO', '123');
INSERT INTO Usuario (ID, Nome, Email, DataNascimento, TipoDeUsuario, Departamento, Telefone, DataCadastro, Status, Senha) VALUES
(usuario_seq.NEXTVAL, 'William Santos', 'williamsantos@gmail.com', TO_DATE('25/08/2005', 'DD/MM/YYYY'), 'TECNICO', 'TI', '19123451025', TO_DATE('09/05/2025', 'DD/MM/YYYY'), 'ATIVO', '123');
INSERT INTO Usuario (ID, Nome, Email, DataNascimento, TipoDeUsuario, Departamento, Telefone, DataCadastro, Status, Senha) VALUES
(usuario_seq.NEXTVAL, 'Richard Bilis', 'richardbilis@gmail.com', TO_DATE('26/04/2003', 'DD/MM/YYYY'), 'TECNICO', 'TI', '19784513698', TO_DATE('09/05/2025', 'DD/MM/YYYY'), 'ATIVO', '123');
INSERT INTO Usuario (ID, Nome, Email, DataNascimento, TipoDeUsuario, Departamento, Telefone, DataCadastro, Status, Senha) VALUES
(usuario_seq.NEXTVAL, 'Carmen Oliveira', 'carmenoliveira@gmail.com', TO_DATE('26/08/2005', 'DD/MM/YYYY'), 'TECNICO', 'TI', '19546321456', TO_DATE('09/05/2025', 'DD/MM/YYYY'), 'ATIVO', '123');
INSERT INTO Usuario (ID, Nome, Email, DataNascimento, TipoDeUsuario, Departamento, Telefone, DataCadastro, Status, Senha) VALUES
(usuario_seq.NEXTVAL, 'Lucas Ferreira', 'lucasferreira@gmail.com', TO_DATE('05/04/1998', 'DD/MM/YYYY'), 'COMUM', 'Administrativo', '19258640214', TO_DATE('09/05/2025', 'DD/MM/YYYY'), 'ATIVO', '123');


INSERT INTO Habilidade_Tecnico (ID_Tecnico, ID_Habilidade) VALUES (1, 11);
INSERT INTO Habilidade_Tecnico (ID_Tecnico, ID_Habilidade) VALUES (1, 7);
INSERT INTO Habilidade_Tecnico (ID_Tecnico, ID_Habilidade) VALUES (1, 2);
INSERT INTO Habilidade_Tecnico (ID_Tecnico, ID_Habilidade) VALUES (5, 6);
INSERT INTO Habilidade_Tecnico (ID_Tecnico, ID_Habilidade) VALUES (5, 4);
INSERT INTO Habilidade_Tecnico (ID_Tecnico, ID_Habilidade) VALUES (6, 2);
INSERT INTO Habilidade_Tecnico (ID_Tecnico, ID_Habilidade) VALUES (6, 3);
INSERT INTO Habilidade_Tecnico (ID_Tecnico, ID_Habilidade) VALUES (7, 8);
INSERT INTO Habilidade_Tecnico (ID_Tecnico, ID_Habilidade) VALUES (8, 7);
INSERT INTO Habilidade_Tecnico (ID_Tecnico, ID_Habilidade) VALUES (8, 3);
INSERT INTO Habilidade_Tecnico (ID_Tecnico, ID_Habilidade) VALUES (9, 5);
INSERT INTO Habilidade_Tecnico (ID_Tecnico, ID_Habilidade) VALUES (9, 9);
INSERT INTO Habilidade_Tecnico (ID_Tecnico, ID_Habilidade) VALUES (9, 11);
INSERT INTO Habilidade_Tecnico (ID_Tecnico, ID_Habilidade) VALUES (10, 1);
INSERT INTO Habilidade_Tecnico (ID_Tecnico, ID_Habilidade) VALUES (10, 4);
INSERT INTO Habilidade_Tecnico (ID_Tecnico, ID_Habilidade) VALUES (10, 12);
INSERT INTO Habilidade_Tecnico (ID_Tecnico, ID_Habilidade) VALUES (11, 10);
INSERT INTO Habilidade_Tecnico (ID_Tecnico, ID_Habilidade) VALUES (11, 2);
INSERT INTO Habilidade_Tecnico (ID_Tecnico, ID_Habilidade) VALUES (11, 6);
