#!/bin/bash

# =============================================================================
# DeliveryApp - Start All Services
# =============================================================================
# This script kills any existing processes and starts all 3 microservices:
# - Order API (port 5002)
# - Billing Worker (listens to order-topic)
# - Delivery Worker (listens to billing-topic)
# =============================================================================

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Set environment to Development to load appsettings.Development.json
export ASPNETCORE_ENVIRONMENT=Development
export DOTNET_ENVIRONMENT=Development

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
MAGENTA='\033[0;35m'
NC='\033[0m' # No Color

echo -e "${YELLOW}======================================${NC}"
echo -e "${YELLOW}  DeliveryApp - Starting All Services ${NC}"
echo -e "${YELLOW}======================================${NC}"
echo ""

# Step 1: Kill existing processes
echo -e "${RED}[1/4] Stopping existing processes...${NC}"
pkill -f "dotnet.*Order" 2>/dev/null
pkill -f "dotnet.*Billing" 2>/dev/null
pkill -f "dotnet.*Delivery" 2>/dev/null
sleep 2
echo -e "      ✓ Existing processes stopped"
echo ""

# Step 2: Start Order API
echo -e "${GREEN}[2/4] Starting Order API (port 5002)...${NC}"
cd "$SCRIPT_DIR/Order"
dotnet run --project Order.csproj > /dev/null 2>&1 &
ORDER_PID=$!
sleep 3
echo -e "      ✓ Order API started (PID: $ORDER_PID)"
echo ""

# Step 3: Start Billing Worker
echo -e "${CYAN}[3/4] Starting Billing Worker...${NC}"
cd "$SCRIPT_DIR/Billing"
dotnet run --project Billing.csproj > /dev/null 2>&1 &
BILLING_PID=$!
sleep 3
echo -e "      ✓ Billing Worker started (PID: $BILLING_PID)"
echo ""

# Step 4: Start Delivery Worker
echo -e "${MAGENTA}[4/4] Starting Delivery Worker...${NC}"
cd "$SCRIPT_DIR/Delivery"
dotnet run --project Delivery.csproj > /dev/null 2>&1 &
DELIVERY_PID=$!
sleep 3
echo -e "      ✓ Delivery Worker started (PID: $DELIVERY_PID)"
echo ""

echo -e "${YELLOW}======================================${NC}"
echo -e "${GREEN}  All services started successfully!  ${NC}"
echo -e "${YELLOW}======================================${NC}"
echo ""
echo -e "  Services running:"
echo -e "  • Order API:       ${GREEN}http://localhost:5002${NC} (PID: $ORDER_PID)"
echo -e "  • Swagger UI:      ${GREEN}http://localhost:5002/swagger${NC}"
echo -e "  • Billing Worker:  ${CYAN}Listening on order-topic${NC} (PID: $BILLING_PID)"
echo -e "  • Delivery Worker: ${MAGENTA}Listening on billing-topic${NC} (PID: $DELIVERY_PID)"
echo ""
echo -e "  Test with:"
echo -e "  ${YELLOW}curl -X POST http://localhost:5002/api/orders \\${NC}"
echo -e "  ${YELLOW}  -H \"Content-Type: application/json\" \\${NC}"
echo -e "  ${YELLOW}  -d '{\"restaurantId\":\"REST-1234\",\"customerPhone\":\"0722123456\",\"deliveryAddress\":\"123 Main St\",\"orderAmount\":99.99}'${NC}"
echo ""
echo -e "  To stop all services, run: ${RED}./stop-all.sh${NC}"
echo ""
