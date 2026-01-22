#!/bin/bash

# =============================================================================
# DeliveryApp - Stop All Services
# =============================================================================

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}======================================${NC}"
echo -e "${YELLOW}  DeliveryApp - Stopping All Services ${NC}"
echo -e "${YELLOW}======================================${NC}"
echo ""

echo -e "${RED}Stopping Order API...${NC}"
pkill -f "dotnet.*Order" 2>/dev/null && echo -e "      ✓ Order API stopped" || echo -e "      - Order API was not running"

echo -e "${RED}Stopping Billing Worker...${NC}"
pkill -f "dotnet.*Billing" 2>/dev/null && echo -e "      ✓ Billing Worker stopped" || echo -e "      - Billing Worker was not running"

echo -e "${RED}Stopping Delivery Worker...${NC}"
pkill -f "dotnet.*Delivery" 2>/dev/null && echo -e "      ✓ Delivery Worker stopped" || echo -e "      - Delivery Worker was not running"

echo ""
echo -e "${GREEN}All services stopped.${NC}"
echo ""
